using CaseAuth.Api.Entities;

namespace CaseAuth.Api.Pipeline;

public record ExtractedFieldResult(string FieldName, string FieldValue, double? Confidence);

// Provisional - owned by Teammate 1 per the project brief ("TextractExtractor: uses AnalyzeID
// for IDs, and AnalyzeDocument with Queries for the W-9, application, and beneficial ownership
// form" / "FixtureExtractor: returns pre-recorded results"). This exact signature is a guess at
// what their real implementation needs; expect it to change once they're building against it.
public interface IDocumentExtractor
{
    Task<IReadOnlyList<ExtractedFieldResult>> ExtractAsync(Document document, CancellationToken ct);
}

// Registered by default until Teammate 1's real extractors exist. Returns nothing - this lets
// the Extract pipeline step run end to end (and the case transition happen) without fabricating
// field data that would look like a real extraction result.
public class FixtureDocumentExtractor : IDocumentExtractor
{
    public Task<IReadOnlyList<ExtractedFieldResult>> ExtractAsync(Document document, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ExtractedFieldResult>>([]);
}
