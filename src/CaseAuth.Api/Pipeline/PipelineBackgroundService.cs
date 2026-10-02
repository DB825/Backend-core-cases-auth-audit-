using CaseAuth.Api.Data;
using CaseAuth.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CaseAuth.Api.Pipeline;

// Polls the ProcessingJob queue table on a timer and hands each Pending row to
// IPipelineJobProcessor. Each job gets its own DI scope (and therefore its own DbContext), so
// one job's failure or slow I/O can't block or poison another's, and each job's work commits in
// its own SaveChangesAsync rather than being batched together.
public class PipelineBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<PipelineOptions> options,
    ILogger<PipelineBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollInterval = TimeSpan.FromSeconds(Math.Max(1, options.Value.PollIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingJobsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Pipeline background service poll iteration failed");
            }

            try
            {
                await Task.Delay(pollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown.
            }
        }
    }

    private async Task ProcessPendingJobsAsync(CancellationToken ct)
    {
        List<Guid> pendingJobIds;
        using (var queryScope = scopeFactory.CreateScope())
        {
            var db = queryScope.ServiceProvider.GetRequiredService<CaseAuthDbContext>();
            pendingJobIds = await db.ProcessingJobs
                .Where(j => j.Status == PipelineJobStatus.Pending)
                .OrderBy(j => j.CreatedAt)
                .Select(j => j.Id)
                .Take(options.Value.MaxJobsPerPoll)
                .ToListAsync(ct);
        }

        foreach (var jobId in pendingJobIds)
        {
            using var jobScope = scopeFactory.CreateScope();
            var processor = jobScope.ServiceProvider.GetRequiredService<IPipelineJobProcessor>();
            await processor.ProcessAsync(jobId, ct);
        }
    }
}
