using System.ComponentModel.DataAnnotations;
using CaseAuth.Api.Entities;

namespace CaseAuth.Api.Contracts;

public record DocumentResponse(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    DocumentType DocumentType,
    string UploadedByUserId,
    DateTime UploadedAt)
{
    public static DocumentResponse From(Document d) => new(
        d.Id, d.FileName, d.ContentType, d.SizeBytes, d.DocumentType, d.UploadedByUserId, d.UploadedAt);
}

public record ExtractedFieldInput(
    [Required, MaxLength(200)] string FieldName,
    [Required, MaxLength(2000)] string FieldValue,
    double? Confidence);

public record CreateExtractedFieldsRequest([Required, MinLength(1)] List<ExtractedFieldInput> Fields);

public record ExtractedFieldResponse(Guid Id, string FieldName, string FieldValue, double? Confidence, DateTime ExtractedAt)
{
    public static ExtractedFieldResponse From(ExtractedField f) => new(f.Id, f.FieldName, f.FieldValue, f.Confidence, f.ExtractedAt);
}
