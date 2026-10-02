using CaseAuth.Api.Auth;
using CaseAuth.Api.Data;
using CaseAuth.Api.Entities;
using CaseAuth.Api.Infrastructure;

namespace CaseAuth.Api.Services;

public interface IAuditService
{
    // Queues an audit event on the given context without saving - callers add this inside the
    // same unit of work as the state change it documents, so one SaveChanges commits both or
    // neither. `metadata` must never carry applicant PII (names, SSNs, DOB, contact info).
    void Record(
        CaseAuthDbContext db,
        Guid? caseId,
        string action,
        AuditOutcome outcome,
        int? aiReviewVersion = null,
        string? metadata = null);
}

public class AuditService(ICurrentUser currentUser, ICorrelationIdAccessor correlationId) : IAuditService
{
    public void Record(
        CaseAuthDbContext db,
        Guid? caseId,
        string action,
        AuditOutcome outcome,
        int? aiReviewVersion = null,
        string? metadata = null)
    {
        db.AuditEvents.Add(new AuditEvent
        {
            CaseId = caseId,
            FirmId = currentUser.FirmId,
            ActorUserId = currentUser.UserId,
            ActorUsername = currentUser.Username,
            Action = action,
            Outcome = outcome,
            CorrelationId = correlationId.CorrelationId,
            AiReviewVersion = aiReviewVersion,
            Metadata = metadata,
        });
    }
}
