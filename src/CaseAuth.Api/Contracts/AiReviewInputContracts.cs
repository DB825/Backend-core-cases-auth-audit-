using CaseAuth.Api.Entities;

namespace CaseAuth.Api.Contracts;

// Assembled for Teammate 4's AI reviewer, per the architecture doc: "Input: extracted fields
// and rule findings only. Document text is untrusted." This is a read-only projection across
// Document/ExtractedField/Finding - nothing here is itself persisted.

public record AiReviewInputFieldResponse(
    Guid Id,
    Guid DocumentId,
    DocumentType DocumentType,
    string FieldName,
    string FieldValue,
    double? Confidence);

public record AiReviewInputFindingResponse(
    Guid Id,
    string Code,
    FindingSeverity Severity,
    double? Score,
    string Message,
    List<Guid> SourceFieldIds);

public record AiReviewInputResponse(
    Guid CaseId,
    List<AiReviewInputFieldResponse> Fields,
    List<AiReviewInputFindingResponse> Findings);
