using System.Net.Http.Json;
using CaseAuth.Api.Data;
using CaseAuth.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CaseAuth.Api.Pipeline;

// Calls the AI Review Agent service (../../../AI-Review-Agent, Teammate 4's Bedrock-backed
// reviewer) over HTTP rather than in-process: it's a separately deployed .NET service with its
// own solution, tests, and eval harness, so this stays a network seam the same way
// IFileStorageService's S3 mode would be - not a merge of the two solutions.
//
// AiReview (this repo's entity) only has ModelName/ModelVersion/Recommendation/Rationale - far
// thinner than AiReviewAgentRecord's summary/key_concerns/recommended_next_steps/draft_case_note
// plus computed confidence (see README: "expect that entity/contract to grow"). Until AiReview
// grows those columns, Rationale carries a flattened version of the richer output so none of it
// is silently dropped.
public class RemoteAiReviewer(HttpClient httpClient, CaseAuthDbContext db, ILogger<RemoteAiReviewer> logger) : IAiReviewer
{
    public async Task<AiReviewResult> ReviewAsync(Guid caseId, CancellationToken ct)
    {
        var request = await BuildRequestAsync(caseId, ct);

        logger.LogInformation(
            "Calling AI Review Agent for case {CaseId}: {DocumentCount} documents, {FindingCount} findings",
            caseId, request.Documents.Count, request.Findings.Count);

        using var response = await httpClient.PostAsJsonAsync($"cases/{request.CaseId}/reviews", request, ct);
        response.EnsureSuccessStatusCode();

        var record = await response.Content.ReadFromJsonAsync<AiReviewAgentRecord>(cancellationToken: ct)
            ?? throw new InvalidOperationException("AI Review Agent returned an empty response body.");

        return new AiReviewResult(
            ModelName: record.IsFallback ? $"ai-review-agent:{record.Provider}-fallback" : $"ai-review-agent:{record.Provider}",
            ModelVersion: record.ModelId ?? record.PromptVersion,
            // The agent only ever proposes - its output never recommends "approve" or "reject"
            // (AiReview.Contracts.Output.ReviewOutput) - so Escalate is the only honest mapping
            // here. The analyst makes the real call through the separate /decisions endpoint.
            Recommendation: AiRecommendation.Escalate,
            Rationale: BuildRationale(record));
    }

    private async Task<AiReviewAgentRequest> BuildRequestAsync(Guid caseId, CancellationToken ct)
    {
        var fields = await db.ExtractedFields
            .Where(f => f.Document!.CaseId == caseId)
            .Select(f => new { f.Id, f.DocumentId, f.Document!.DocumentType, f.FieldName, f.FieldValue, f.Confidence })
            .ToListAsync(ct);

        var findings = await db.Findings
            .Where(f => f.CaseId == caseId)
            .Select(f => new { f.Id, f.Code, f.Severity, f.Score, f.Message, SourceFieldIds = f.SourceFields.Select(sf => sf.Id).ToList() })
            .ToListAsync(ct);

        // field_id is "{document_id}.{name}" per AiReview.Contracts.Input.ExtractedField -
        // build it once so documents and findings reference the exact same string.
        var fieldIdsById = fields.ToDictionary(f => f.Id, f => $"{f.DocumentId}.{f.FieldName}");

        var documents = fields
            .GroupBy(f => new { f.DocumentId, f.DocumentType })
            .Select(g =>
            {
                var agentFields = g
                    .Select(f => new AiReviewAgentField(
                        FieldId: fieldIdsById[f.Id],
                        Name: f.FieldName,
                        Value: f.FieldValue,
                        // Textract confidence is 0-100; this repo stores it as 0.0-1.0 (ExtractedField.Confidence).
                        TextractConfidence: (f.Confidence ?? 0) * 100))
                    .ToList();

                return new AiReviewAgentDocument(
                    DocumentId: g.Key.DocumentId.ToString(),
                    DocumentType: g.Key.DocumentType.ToString(),
                    Fields: agentFields);
            })
            .ToList();

        var agentFindings = findings
            .Select(f => new AiReviewAgentFinding(
                FindingId: f.Id.ToString(),
                RuleId: f.Code,
                Severity: f.Severity.ToString().ToLowerInvariant(),
                // Full confidence when the rules engine didn't attach a score - the finding
                // still fired, it's just not weighted (see Finding.Score's own doc comment).
                MatchScore: f.Score ?? 1.0,
                Description: f.Message,
                FieldIds: f.SourceFieldIds.Select(id => fieldIdsById[id]).ToList()))
            .ToList();

        return new AiReviewAgentRequest(caseId.ToString(), documents, agentFindings);
    }

    private static string BuildRationale(AiReviewAgentRecord record)
    {
        var lines = new List<string> { record.Output.Summary };

        if (record.Output.KeyConcerns.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("Key concerns:");
            lines.AddRange(record.Output.KeyConcerns.Select(c => $"- {c.Concern}"));
        }

        if (record.Output.RecommendedNextSteps.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("Recommended next steps:");
            lines.AddRange(record.Output.RecommendedNextSteps.Select(s => $"- {s}"));
        }

        lines.Add(string.Empty);
        lines.Add($"Confidence: {record.Confidence.Band} ({record.Confidence.Score:0.00})");

        if (record.IsFallback)
        {
            lines.Add($"Fallback review: generated from rules, not AI ({record.FallbackReason}).");
        }

        return string.Join('\n', lines);
    }
}
