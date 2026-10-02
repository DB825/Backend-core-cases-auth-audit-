using CaseAuth.Api.Entities;

namespace CaseAuth.Api.Contracts;

public record AuditEventResponse(
    Guid Id,
    Guid? CaseId,
    string ActorUsername,
    string Action,
    AuditOutcome Outcome,
    DateTime Timestamp,
    string CorrelationId,
    int? AiReviewVersion)
{
    public static AuditEventResponse From(AuditEvent e) => new(
        e.Id, e.CaseId, e.ActorUsername, e.Action, e.Outcome, e.Timestamp, e.CorrelationId, e.AiReviewVersion);
}
