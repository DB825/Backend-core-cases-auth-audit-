namespace CaseAuth.Api.Entities;

public class Decision
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CaseId { get; set; }
    public Case? Case { get; set; }

    public DecisionOutcome Outcome { get; set; }

    public Guid? AiReviewId { get; set; }
    public AiReview? AiReview { get; set; }

    public required string DecidedByUserId { get; set; }

    // Unique per (CaseId, IdempotencyKey) - see DecisionsController for replay semantics.
    public required string IdempotencyKey { get; set; }

    public DateTime DecidedAt { get; set; } = DateTime.UtcNow;
}
