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

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
