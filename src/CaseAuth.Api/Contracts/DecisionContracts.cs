using System.ComponentModel.DataAnnotations;
using CaseAuth.Api.Entities;

namespace CaseAuth.Api.Contracts;

public record CreateDecisionRequest(
    [Required] DecisionOutcome Outcome,
    Guid? AiReviewId);

public record DecisionResponse(
    Guid Id,
    Guid CaseId,
    DecisionOutcome Outcome,
    Guid? AiReviewId,
    string DecidedByUserId,
    DateTime DecidedAt)
{
    public static DecisionResponse From(Decision d) => new(
        d.Id, d.CaseId, d.Outcome, d.AiReviewId, d.DecidedByUserId, d.DecidedAt);
}
