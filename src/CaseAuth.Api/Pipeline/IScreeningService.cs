using CaseAuth.Api.Entities;

namespace CaseAuth.Api.Pipeline;

public record ScreeningFindingResult(
    string Code,
    FindingSeverity Severity,
    string Message,
    double? Score,
    IReadOnlyList<Guid> SourceFieldIds);

// Provisional - owned by Teammate 3 per the project brief (name matching, cross-document
// consistency, validity checks, sanctions screening, red flags). This exact signature is a
// guess at what their deterministic rules engine needs; expect it to change once they're
// building against it.
public interface IScreeningService
{
    Task<IReadOnlyList<ScreeningFindingResult>> ScreenAsync(Guid caseId, CancellationToken ct);
}

// Registered by default until Teammate 3's rules engine exists. Returns no findings.
public class FixtureScreeningService : IScreeningService
{
    public Task<IReadOnlyList<ScreeningFindingResult>> ScreenAsync(Guid caseId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ScreeningFindingResult>>([]);
}
