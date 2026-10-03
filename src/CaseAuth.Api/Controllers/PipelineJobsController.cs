using System.ComponentModel.DataAnnotations;
using CaseAuth.Api.Contracts;
using CaseAuth.Api.Data;
using CaseAuth.Api.Entities;
using CaseAuth.Api.Errors;
using CaseAuth.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CaseAuth.Api.Controllers;

// Enqueues idempotent work for PipelineBackgroundService; the worker owns processing and state transitions.
[ApiController]
[Route("api/cases/{caseId:guid}/pipeline-jobs")]
[Authorize]
public class PipelineJobsController(CaseAuthDbContext db, ICaseAccessor caseAccessor, IAuditService audit) : ControllerBase
{
    public const string IdempotencyKeyHeader = "Idempotency-Key";

    [HttpGet]
    public async Task<ActionResult<List<PipelineJobResponse>>> List(Guid caseId, CancellationToken ct)
    {
        await caseAccessor.GetScopedCaseAsync(db, caseId, ct);
        var jobs = await db.ProcessingJobs
            .Where(j => j.CaseId == caseId)
            .OrderBy(j => j.CreatedAt)
            .ToListAsync(ct);
        return jobs.Select(PipelineJobResponse.From).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<PipelineJobResponse>> Enqueue(
        Guid caseId,
        [FromHeader(Name = IdempotencyKeyHeader), Required] string idempotencyKey,
        [FromBody] EnqueuePipelineJobRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 200)
        {
            throw new ValidationApiException(
                $"The '{IdempotencyKeyHeader}' header is required and must not exceed 200 characters.");
        }

        await caseAccessor.GetScopedCaseAsync(db, caseId, ct);

        var existing = await db.ProcessingJobs.AsNoTracking()
            .FirstOrDefaultAsync(job => job.CaseId == caseId && job.IdempotencyKey == idempotencyKey, ct);
        if (existing is not null)
        {
            if (existing.JobType != request.JobType)
            {
                throw new ConflictApiException(
                    $"Idempotency key '{idempotencyKey}' was already used for this case with a different job type.");
            }

            return Ok(PipelineJobResponse.From(existing));
        }

        var job = new ProcessingJob
        {
            CaseId = caseId,
            JobType = request.JobType,
            IdempotencyKey = idempotencyKey,
        };
        db.ProcessingJobs.Add(job);
        audit.Record(db, caseId, $"PipelineJob.Enqueued.{request.JobType}", AuditOutcome.Success);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex is not DbUpdateConcurrencyException)
        {
            // The unique (CaseId, IdempotencyKey) index arbitrates concurrent retries.
            var winner = await db.ProcessingJobs.AsNoTracking()
                .FirstOrDefaultAsync(candidate =>
                    candidate.CaseId == caseId && candidate.IdempotencyKey == idempotencyKey, ct);
            if (winner is null)
            {
                throw;
            }

            if (winner.JobType != request.JobType)
            {
                throw new ConflictApiException(
                    $"Idempotency key '{idempotencyKey}' was already used for this case with a different job type.");
            }

            return Ok(PipelineJobResponse.From(winner));
        }

        return AcceptedAtAction(nameof(List), new { caseId }, PipelineJobResponse.From(job));
    }
}
