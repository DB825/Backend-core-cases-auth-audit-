namespace CaseAuth.Api.Entities;

public enum CaseStatus
{
    Draft,
    Submitted,
    UnderReview,
    PendingDecision,
    Approved,
    Rejected,
    Withdrawn,
}

public enum DocumentType
{
    GovernmentId,
    ProofOfAddress,
    Financial,
    Other,
}

public enum FindingSeverity
{
    Info,
    Warning,
    Critical,
}

public enum FindingSource
{
    Ai,
    Manual,
}

public enum AiRecommendation
{
    Approve,
    Reject,
    Escalate,
}

public enum DecisionOutcome
{
    Approved,
    Rejected,
}

public enum AuditOutcome
{
    Success,
    Rejected,
    Failure,
}
