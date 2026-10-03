using CaseAuth.Api.Contracts;
using CaseAuth.Api.Data;
using CaseAuth.Api.Entities;
using CaseAuth.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CaseAuth.Api.Controllers;

// Enqueues work for PipelineBackgroundService instead of doing it synchronously. This is a
// parallel, opt-in path alongside CasesController's existing /extract, /screen, and
// /mark-ai-reviewed endpoints - those are unchanged and still do their transition synchronously.
// Once Teammates 1/3/4's real IDocumentExtractor/IScreeningService/IAiReviewer implementations
// exist, whoever owns the demo flow can decide whether to switch callers over to this queued
// path or keep triggering steps manually.
[ApiController]
[Route("api/cases/{caseId:guid}/pipeline-jobs")]
[Authorize]
public class PipelineJobsController(CaseAuthDbContext db, ICaseAccessor caseAccessor, IAuditService audit) : ControllerBase
{
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
        Guid caseId, [FromBody] EnqueuePipelineJobRequest request, CancellationToken ct)
    {
        var c = await caseAccessor.GetScopedCaseAsync(db, caseId, ct);

        var job = new ProcessingJob { CaseId = c.Id, JobType = request.JobType };
        db.ProcessingJobs.Add(job);
        audit.Record(db, c.Id, $"PipelineJob.Enqueued.{request.JobType}", AuditOutcome.Success);
        await db.SaveChangesAsync(ct);

        return AcceptedAtAction(nameof(List), new { caseId }, PipelineJobResponse.From(job));
    }
}
