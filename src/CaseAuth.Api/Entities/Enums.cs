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

// Low/Medium/High (not Info/Warning/Critical) to match the vocabulary Teammate 3's screening
// engine and Teammate 4's AI reviewer are built against.
public enum FindingSeverity
{
    Low,
    Medium,
    High,
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
