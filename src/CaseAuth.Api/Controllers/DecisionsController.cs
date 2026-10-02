using CaseAuth.Api.Auth;
using CaseAuth.Api.Contracts;
using CaseAuth.Api.Data;
using CaseAuth.Api.Entities;
using CaseAuth.Api.Errors;
using CaseAuth.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CaseAuth.Api.Controllers;

[ApiController]
[Route("api/cases/{caseId:guid}/decisions")]
[Authorize]
public class DecisionsController(
    CaseAuthDbContext db,
    ICurrentUser currentUser,
    ICaseAccessor caseAccessor,
    IAuditService audit) : ControllerBase
{
    public const string IdempotencyKeyHeader = "Idempotency-Key";
    public const string IfMatchHeader = "If-Match";

    [HttpGet]
    public async Task<ActionResult<List<DecisionResponse>>> List(Guid caseId, CancellationToken ct)
    {
        await caseAccessor.GetScopedCaseAsync(db, caseId, ct);
        var decisions = await db.Decisions
            .Where(d => d.CaseId == caseId)
            .OrderBy(d => d.DecidedAt)
            .ToListAsync(ct);
        return decisions.Select(DecisionResponse.From).ToList();
    }

    [HttpPost]
    [Authorize(Roles = Roles.Supervisor)]
    public async Task<ActionResult<DecisionResponse>> Create(
        Guid caseId, [FromBody] CreateDecisionRequest request, CancellationToken ct)
    {
        var idempotencyKey = Request.Headers[IdempotencyKeyHeader].ToString();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ValidationApiException($"The '{IdempotencyKeyHeader}' header is required.");
        }

        var ifMatch = Request.Headers[IfMatchHeader].ToString();
        if (!Guid.TryParse(ifMatch, out var expectedRowVersion))
        {
            throw new ValidationApiException(
                $"The '{IfMatchHeader}' header must carry the case's current RowVersion (a guid).");
        }

        var c = await caseAccessor.GetScopedCaseAsync(db, caseId, ct);

        // Idempotency replay: a retried request with the same key returns the original result
        // without writing a second Decision or AuditEvent row.
        var existing = await db.Decisions
            .FirstOrDefaultAsync(d => d.CaseId == caseId && d.IdempotencyKey == idempotencyKey, ct);
        if (existing is not null)
        {
            if (existing.Outcome == request.Outcome && existing.AiReviewId == request.AiReviewId)
            {
                return Ok(DecisionResponse.From(existing));
            }

            throw new ConflictApiException(
                $"Idempotency key '{idempotencyKey}' was already used for this case with a different outcome.");
        }

        if (expectedRowVersion != c.RowVersion)
        {
            throw new ConflictApiException(
                "The case has been modified since it was loaded. Reload it and retry with its current RowVersion.");
        }

        if (c.Status != CaseStatus.PendingDecision)
        {
            throw new ConflictApiException(
                $"A decision can only be recorded while the case is PendingDecision (current status: {c.Status}).");
        }

        int? aiReviewVersion = null;
        if (request.AiReviewId is { } aiReviewId)
        {
            var aiReview = await db.AiReviews.FirstOrDefaultAsync(r => r.Id == aiReviewId && r.CaseId == caseId, ct);
            if (aiReview is null)
            {
                throw new ValidationApiException($"AI review '{aiReviewId}' does not belong to this case.");
            }

            aiReviewVersion = aiReview.Version;
        }

        c.Status = request.Outcome == DecisionOutcome.Approved ? CaseStatus.Approved : CaseStatus.Rejected;

        var decision = new Decision
        {
            CaseId = c.Id,
            Outcome = request.Outcome,
            AiReviewId = request.AiReviewId,
            DecidedByUserId = currentUser.UserId,
            IdempotencyKey = idempotencyKey,
        };
        db.Decisions.Add(decision);

        audit.Record(
            db, c.Id,
            request.Outcome == DecisionOutcome.Approved ? "Decision.Approved" : "Decision.Rejected",
            AuditOutcome.Success,
            aiReviewVersion: aiReviewVersion);

        try
        {
            // One SaveChangesAsync call commits the status change, the Decision row, and the
            // AuditEvent together, or none of them. EF's concurrency token (RowVersion) and the
            // unique (CaseId, IdempotencyKey) index both guard this single call: a stale
            // RowVersion throws DbUpdateConcurrencyException, and a racing duplicate submission
            // throws a unique-constraint DbUpdateException - either way nothing partial commits.
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex is not DbUpdateConcurrencyException)
        {
            // Only a concurrent duplicate submission with the same idempotency key should hit
            // the unique index here (the RowVersion race is DbUpdateConcurrencyException, left
            // to the ApiExceptionMiddleware to turn into 409). Replay the winner's result rather
            // than surfacing a raw constraint error.
            var winner = await db.Decisions
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.CaseId == caseId && d.IdempotencyKey == idempotencyKey, ct);
            if (winner is not null && winner.Outcome == request.Outcome && winner.AiReviewId == request.AiReviewId)
            {
                return Ok(DecisionResponse.From(winner));
            }

            throw new ConflictApiException(
                $"Idempotency key '{idempotencyKey}' was already used for this case with a different outcome.");
        }

        return CreatedAtAction(nameof(List), new { caseId }, DecisionResponse.From(decision));
    }
}
