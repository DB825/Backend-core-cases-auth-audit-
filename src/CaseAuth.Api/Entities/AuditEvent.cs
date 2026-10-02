namespace CaseAuth.Api.Entities;

// Append-only by convention (no update/delete endpoints exposed). Metadata must never contain
// applicant PII - see Services/AuditService for the write path and redaction contract.
public class AuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? CaseId { get; set; }

    public required string FirmId { get; set; }
    public required string ActorUserId { get; set; }
    public required string ActorUsername { get; set; }

    public required string Action { get; set; }
    public AuditOutcome Outcome { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public required string CorrelationId { get; set; }

    // The AiReview.Version that was approved, when Action is a decision event.
    public int? AiReviewVersion { get; set; }

    public string? Metadata { get; set; }
}
