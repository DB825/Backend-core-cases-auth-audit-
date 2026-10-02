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
        new("submit", CaseStatus.Draft, CaseStatus.Submitted, Roles.Analyst),
        new("start-review", CaseStatus.Submitted, CaseStatus.UnderReview, Roles.Analyst),
        new("request-decision", CaseStatus.UnderReview, CaseStatus.PendingDecision, Roles.Analyst),
        new("withdraw", CaseStatus.Draft, CaseStatus.Withdrawn, Roles.Analyst),
        new("withdraw", CaseStatus.Submitted, CaseStatus.Withdrawn, Roles.Analyst),
        new("withdraw", CaseStatus.UnderReview, CaseStatus.Withdrawn, Roles.Analyst),
        // Approved/Rejected are reached only through DecisionsController, which carries its
        // own idempotency and concurrency rules and performs this transition itself.
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
