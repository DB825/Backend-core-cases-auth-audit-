namespace CaseAuth.Api.Entities;

public class Applicant
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string FirmId { get; set; }

    public required string FullName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    public Case? Case { get; set; }
}
