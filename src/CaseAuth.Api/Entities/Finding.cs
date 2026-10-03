namespace CaseAuth.Api.Entities;

public class Finding
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CaseId { get; set; }
    public Case? Case { get; set; }

    public FindingSeverity Severity { get; set; }
    public FindingSource Source { get; set; }
    public required string Code { get; set; }
    public required string Message { get; set; }

    // Teammate 3's weighted-sum rule score (e.g. 0.89 for a near-certain name mismatch).
    // Nullable because not every finding comes from a scored rule (a manual analyst note
    // wouldn't have one).
    public double? Score { get; set; }

    // The ExtractedField row(s) this finding was computed from - a many-to-many join, since a
    // cross-document check (e.g. address mismatch between an ID and a W-9) cites a field from
    // each document, not just one. Required by acceptance criterion #1 ("findings link back to
    // their source fields") and the case-detail UI.
    public List<ExtractedField> SourceFields { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? EvidenceJson { get; set; }
}
