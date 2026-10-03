using System.Text.Json;
using System.Text.Json.Serialization;
using CaseAuth.Api.Entities;

namespace CaseAuth.Api.Demo;

// The shape of demo/personas.json - the same file demo/seed.mjs loads over HTTP and that the
// other modules use as test fixtures. Everything in it is synthetic.
public record PersonaFile(List<Persona> Personas);

public record Persona(
    string Key,
    string Label,
    PersonaApplicant Applicant,
    List<PersonaDocument> Documents,
    List<PersonaFinding> Findings,
    PersonaAiReview AiReview);

public record PersonaApplicant(string FullName, DateOnly? DateOfBirth, string? Email, string? Phone);

public record PersonaDocument(string Key, DocumentType Type, string Title, string File, List<PersonaField> Fields);

public record PersonaField(string Name, string Value, double? Confidence);

// `Fields` are "docKey.FIELD_NAME" references into the persona's own documents.
public record PersonaFinding(string Code, FindingSeverity Severity, double? Score, string Message, List<string> Fields);

public record PersonaAiReview(
    AiRecommendation Recommendation,
    string Summary,
    List<PersonaConcern> KeyConcerns,
    List<string> NextSteps,
    string DraftCaseNote);

public record PersonaConcern(string Text, List<string> FindingCodes);

public static class PersonaLoader
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task<List<Persona>> LoadAsync(string path, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        var file = await JsonSerializer.DeserializeAsync<PersonaFile>(stream, JsonOptions, ct)
            ?? throw new InvalidOperationException($"'{path}' is empty.");
        return file.Personas;
    }
}
