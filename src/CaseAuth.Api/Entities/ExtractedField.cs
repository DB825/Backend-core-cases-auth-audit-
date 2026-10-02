namespace CaseAuth.Api.Entities;

public class ExtractedField
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DocumentId { get; set; }
    public Document? Document { get; set; }

    public required string FieldName { get; set; }
    public required string FieldValue { get; set; }
    public double? Confidence { get; set; }

    public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;
}
