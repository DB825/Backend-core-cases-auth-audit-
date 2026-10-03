using CaseAuth.Api.Entities;

namespace CaseAuth.Api.Pipeline;

public record ScreeningFindingResult(
    string Code,
    FindingSeverity Severity,
    string Message,
    double? Score,
    IReadOnlyList<Guid> SourceFieldIds,
    string EvidenceJson);

public sealed record ScreeningEvaluation(
    IReadOnlyList<ScreeningFindingResult> Findings,
    double TotalScore,
    string RiskTier,
    bool IsComplete,
    string RulesetVersion,
    DateOnly EvaluationDate,
    string SnapshotSource,
    DateOnly? SnapshotDate);

// Pipeline owns persistence, audit, and state transitions; the screening implementation only
// returns its deterministic evaluation.
public interface IScreeningService
{
    Task<ScreeningEvaluation> ScreenAsync(Guid caseId, CancellationToken ct);
}

// Empty test/demo implementation for pipeline tests that do not exercise screening.
public class FixtureScreeningService : IScreeningService
{
    public Task<ScreeningEvaluation> ScreenAsync(Guid caseId, CancellationToken ct) =>
        Task.FromResult(new ScreeningEvaluation(
            [], 0, "Low", true, "fixture",
            DateOnly.FromDateTime(DateTime.UtcNow), "No sanctions data", null));
}
