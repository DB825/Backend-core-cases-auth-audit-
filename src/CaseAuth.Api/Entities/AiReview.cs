namespace CaseAuth.Api.Entities;

// Recorded verbatim from an upstream review pipeline; this API does not call a model itself.
// See Services/AiReviewService for the deterministic-recording note.
public class AiReview
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CaseId { get; set; }
    public Case? Case { get; set; }

    // Monotonically increasing per case, starting at 1. Decisions reference this version
    // so the audit trail records exactly which review version was approved against.
    public int Version { get; set; }

    public required string ModelName { get; set; }
    public required string ModelVersion { get; set; }
    public AiRecommendation Recommendation { get; set; }
    public required string Rationale { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
