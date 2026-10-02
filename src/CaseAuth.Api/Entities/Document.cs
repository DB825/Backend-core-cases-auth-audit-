namespace CaseAuth.Api.Entities;

public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CaseId { get; set; }
    public Case? Case { get; set; }

    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public long SizeBytes { get; set; }
    public DocumentType DocumentType { get; set; }

    // Relative path under the fixture storage root, or an S3 object key.
    public required string StorageKey { get; set; }

    public required string UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public List<ExtractedField> ExtractedFields { get; set; } = [];
}
