using System.Text.Json.Serialization;

namespace CaseAuth.Api.Pipeline;

// Wire DTOs for the AI Review Agent service (../../../AI-Review-Agent), kept local rather than
// referencing that repo's AiReview.Contracts project directly - the two are separately deployed
// services (separate repos, separate containers), so each owns its own copy of the contract it
// depends on, the same way an Angular client would. Field names/shapes mirror
// AiReview.Contracts.Input/Output exactly; see that project if the two drift.

public sealed record AiReviewAgentRequest(
    [property: JsonPropertyName("case_id")] string CaseId,
    [property: JsonPropertyName("documents")] IReadOnlyList<AiReviewAgentDocument> Documents,
    [property: JsonPropertyName("findings")] IReadOnlyList<AiReviewAgentFinding> Findings);

public sealed record AiReviewAgentDocument(
    [property: JsonPropertyName("document_id")] string DocumentId,
    [property: JsonPropertyName("document_type")] string DocumentType,
    [property: JsonPropertyName("fields")] IReadOnlyList<AiReviewAgentField> Fields);

public sealed record AiReviewAgentField(
    [property: JsonPropertyName("field_id")] string FieldId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("value")] string Value,
    [property: JsonPropertyName("textract_confidence")] double TextractConfidence);

public sealed record AiReviewAgentFinding(
    [property: JsonPropertyName("finding_id")] string FindingId,
    [property: JsonPropertyName("rule_id")] string RuleId,
    [property: JsonPropertyName("severity")] string Severity,
    [property: JsonPropertyName("match_score")] double MatchScore,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("field_ids")] IReadOnlyList<string> FieldIds);

public sealed record AiReviewAgentRecord(
    [property: JsonPropertyName("review_id")] Guid ReviewId,
    [property: JsonPropertyName("case_id")] string CaseId,
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("provider")] string Provider,
    [property: JsonPropertyName("is_fallback")] bool IsFallback,
    [property: JsonPropertyName("fallback_reason")] string? FallbackReason,
    [property: JsonPropertyName("model_id")] string? ModelId,
    [property: JsonPropertyName("prompt_version")] string PromptVersion,
    [property: JsonPropertyName("output")] AiReviewAgentOutput Output,
    [property: JsonPropertyName("confidence")] AiReviewAgentConfidence Confidence);

public sealed record AiReviewAgentOutput(
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("key_concerns")] IReadOnlyList<AiReviewAgentKeyConcern> KeyConcerns,
    [property: JsonPropertyName("recommended_next_steps")] IReadOnlyList<string> RecommendedNextSteps,
    [property: JsonPropertyName("draft_case_note")] string DraftCaseNote);

public sealed record AiReviewAgentKeyConcern(
    [property: JsonPropertyName("concern")] string Concern,
    [property: JsonPropertyName("cited_finding_ids")] IReadOnlyList<string> CitedFindingIds);

public sealed record AiReviewAgentConfidence(
    [property: JsonPropertyName("score")] double Score,
    [property: JsonPropertyName("band")] string Band);
