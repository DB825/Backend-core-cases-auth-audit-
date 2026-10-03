namespace CaseAuth.Api.Entities;

// The queue table backing the background pipeline: PipelineBackgroundService polls for Pending
// rows and PipelineJobProcessor does the work. See Pipeline/ for the pluggable step interfaces
// (IDocumentExtractor, IScreeningService, IAiReviewer) Teammates 1/3/4 will implement for real.
public class ProcessingJob
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CaseId { get; set; }
    public Case? Case { get; set; }

    public PipelineJobType JobType { get; set; }
    public string? IdempotencyKey { get; set; }
    public PipelineJobStatus Status { get; set; } = PipelineJobStatus.Pending;

    public int Attempts { get; set; }
    public string? Error { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
