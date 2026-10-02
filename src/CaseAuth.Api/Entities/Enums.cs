namespace CaseAuth.Api.Entities;

// Matches the architecture doc's state machine exactly, since Teammates 1/3/4 each drive a
// transition after their pipeline step finishes (extraction, screening, AI review).
public enum CaseStatus
{
    Uploaded,
    Extracted,
    Screened,
    AiReviewed,
    AwaitingDecision,
    Approved,
    Rejected,
    Escalated,
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
    Escalated,
}

public enum AuditOutcome
{
    Success,
    Rejected,
    Failure,
}
