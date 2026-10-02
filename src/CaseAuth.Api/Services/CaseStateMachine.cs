using CaseAuth.Api.Auth;
using CaseAuth.Api.Entities;

namespace CaseAuth.Api.Services;

public record CaseTransition(string Action, CaseStatus From, CaseStatus To, string RequiredRole);

// The single source of truth for which Case.Status transitions are legal, who may trigger
// them, and what each is called in the audit trail. CasesController is a thin wrapper around
// this - it must never mutate Status directly.
public static class CaseStateMachine
{
    public static readonly IReadOnlyList<CaseTransition> Transitions = new List<CaseTransition>
    {
        // The pipeline: Uploaded -> Extracted -> Screened -> AiReviewed -> AwaitingDecision.
        // Each step is triggered here by an analyst for now; Teammates 1/3/4's modules can
        // call the same endpoints once their background pipeline exists.
        new("extract", CaseStatus.Uploaded, CaseStatus.Extracted, Roles.Analyst),
        new("screen", CaseStatus.Extracted, CaseStatus.Screened, Roles.Analyst),
        new("mark-ai-reviewed", CaseStatus.Screened, CaseStatus.AiReviewed, Roles.Analyst),
        new("request-decision", CaseStatus.AiReviewed, CaseStatus.AwaitingDecision, Roles.Analyst),

        // Request documents: send the case back to Uploaded from any point before a decision,
        // so a fresh document triggers the pipeline again from the top.
        new("request-documents", CaseStatus.Extracted, CaseStatus.Uploaded, Roles.Analyst),
        new("request-documents", CaseStatus.Screened, CaseStatus.Uploaded, Roles.Analyst),
        new("request-documents", CaseStatus.AiReviewed, CaseStatus.Uploaded, Roles.Analyst),
        new("request-documents", CaseStatus.AwaitingDecision, CaseStatus.Uploaded, Roles.Analyst),

        // Approved/Rejected/Escalated are reached only through DecisionsController, which
        // carries its own idempotency and concurrency rules and performs this transition itself.
    };

    public static CaseTransition Resolve(string action, CaseStatus from, string role)
    {
        var transition = Transitions.FirstOrDefault(t => t.Action == action && t.From == from);
        if (transition is null)
        {
            throw new Errors.ConflictApiException(
                $"Cannot perform '{action}' while the case is in status '{from}'.");
        }

        if (transition.RequiredRole != role)
        {
            throw new Errors.ForbiddenApiException(
                $"Action '{action}' requires role '{transition.RequiredRole}'.");
        }

        return transition;
    }
}
