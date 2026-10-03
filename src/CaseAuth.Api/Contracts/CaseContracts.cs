using System.ComponentModel.DataAnnotations;
using CaseAuth.Api.Entities;

namespace CaseAuth.Api.Contracts;

public record CreateCaseRequest(
    [Required, MaxLength(200)] string ApplicantFullName,
    DateOnly? ApplicantDateOfBirth,
    [EmailAddress] string? ApplicantEmail,
    string? ApplicantPhone,
    ApplicantKind ApplicantKind = ApplicantKind.Individual);

public record CaseResponse(
    Guid Id,
    string FirmId,
    CaseStatus Status,
    string ApplicantFullName,
    string CreatedByUserId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    Guid RowVersion)
{
    public static CaseResponse From(Case c) => new(
        c.Id, c.FirmId, c.Status, c.Applicant!.FullName, c.CreatedByUserId, c.CreatedAt, c.UpdatedAt, c.RowVersion);
}

public record CreateAiReviewRequest(
    [Required] string ModelName,
    [Required] string ModelVersion,
    [Required] AiRecommendation Recommendation,
    [Required, MaxLength(4000)] string Rationale);

public record AiReviewResponse(
    Guid Id,
    int Version,
    string ModelName,
    string ModelVersion,
    AiRecommendation Recommendation,
    string Rationale,
    DateTime CreatedAt)
{
    public static AiReviewResponse From(AiReview r) => new(
        r.Id, r.Version, r.ModelName, r.ModelVersion, r.Recommendation, r.Rationale, r.CreatedAt);
}

public record CreateFindingRequest(
    [Required] FindingSeverity Severity,
    [Required] FindingSource Source,
    [Required, MaxLength(100)] string Code,
    [Required, MaxLength(2000)] string Message,
    // Teammate 3's weighted-sum rule score, when this finding came from a scored rule.
    [Range(0, 1)] double? Score,
    // The ExtractedField row(s) this finding was computed from - e.g. a cross-document address
    // mismatch cites one field from each document. May be empty for findings that aren't tied
    // to specific fields (e.g. "missing required document").
    List<Guid>? SourceFieldIds);

public record FindingResponse(
    Guid Id,
    FindingSeverity Severity,
    FindingSource Source,
    string Code,
    string Message,
    double? Score,
    List<Guid> SourceFieldIds,
    DateTime CreatedAt,
    string? EvidenceJson = null)
{
    public static FindingResponse From(Finding f) => new(
        f.Id, f.Severity, f.Source, f.Code, f.Message, f.Score, f.SourceFields.Select(sf => sf.Id).ToList(), f.CreatedAt, f.EvidenceJson);
}

public record MeResponse(string UserId, string Username, string FirmId, string Role);
