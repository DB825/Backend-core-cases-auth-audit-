using System.Text.Json.Nodes;
using CaseAuth.Api.Data;
using CaseAuth.Api.Entities;
using CaseAuth.Api.Screening;
using Microsoft.EntityFrameworkCore;

namespace CaseAuth.Api.Pipeline;

public sealed class DeterministicScreeningService(
    CaseAuthDbContext db,
    ScreeningEngine engine,
    ScreeningOptions options,
    SanctionsSnapshot snapshot,
    TimeProvider clock) : IScreeningService
{
    public async Task<ScreeningEvaluation> ScreenAsync(Guid caseId, CancellationToken ct)
    {
        var c = await db.Cases
            .Include(item => item.Applicant)
            .SingleAsync(item => item.Id == caseId, ct);

        var documents = await db.Documents.AsNoTracking()
            .Where(document => document.CaseId == caseId)
            .OrderBy(document => document.Id)
            .Select(document => new ScreeningDocument(document.Id, document.DocumentType))
            .ToArrayAsync(ct);

        var documentIds = documents.Select(document => document.Id).ToArray();
        var fields = await db.ExtractedFields.AsNoTracking()
            .Where(field => documentIds.Contains(field.DocumentId))
            .OrderBy(field => field.DocumentId)
            .ThenBy(field => field.Id)
            .ToArrayAsync(ct);

        var sharedContacts = await FindSharedContactsAsync(c, fields, ct);
        var evaluationDate = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var input = new ScreeningInput(
            c.Id,
            c.Applicant!.Kind,
            c.Applicant.FullName,
            c.Applicant.DateOfBirth,
            evaluationDate,
            documents,
            fields.Select(field => new ScreeningField(
                field.Id,
                field.DocumentId,
                Normalization.Key(field.FieldName),
                field.FieldValue,
                field.Confidence)).ToArray(),
            sharedContacts);

        var result = engine.Evaluate(input, snapshot);
        var contributions = result.Contributions.ToDictionary(item => item.Code);
        var findings = result.Findings.Select(finding =>
        {
            var contribution = contributions[finding.Code];
            var evidence = JsonNode.Parse(finding.EvidenceJson)!.AsObject();
            evidence["ruleWeight"] = contribution.Weight;
            evidence["weightedPoints"] = contribution.Points;

            return new ScreeningFindingResult(
                finding.Code,
                finding.Severity,
                finding.Message,
                finding.Risk,
                finding.SourceFieldIds,
                evidence.ToJsonString());
        }).ToArray();

        return new ScreeningEvaluation(
            findings,
            result.TotalScore,
            result.RiskTier,
            result.IsComplete,
            options.RulesetVersion,
            evaluationDate,
            snapshot.Source,
            snapshot.Date);
    }

    private async Task<SharedContact[]> FindSharedContactsAsync(
        Case current,
        ExtractedField[] fields,
        CancellationToken ct)
    {
        bool Usable(ExtractedField field)
        {
            var key = Normalization.Key(field.FieldName);
            return key is "address" or "phone"
                && field.Confidence is { } confidence
                && double.IsFinite(confidence)
                && confidence >= options.MinimumExtractionConfidence
                && confidence <= 1
                && Normalization.Valid(key, field.FieldValue);
        }

        var own = fields.Where(Usable).ToArray();
        if (own.Length == 0)
        {
            return [];
        }

        // ponytail: compare same-firm contacts in memory for the demo; index canonical values in DB if volume grows.
        var otherFields = await db.ExtractedFields.AsNoTracking()
            .Where(field => field.Document!.CaseId != current.Id
                && field.Document.Case!.FirmId == current.FirmId)
            .Select(field => new { Field = field, CaseId = field.Document!.CaseId })
            .ToArrayAsync(ct);

        var matches = new List<SharedContact>();
        foreach (var source in own)
        {
            var kind = Normalization.Key(source.FieldName);
            var canonical = Normalization.Canonical(kind, source.FieldValue);
            foreach (var other in otherFields)
            {
                if (Usable(other.Field)
                    && Normalization.Key(other.Field.FieldName) == kind
                    && Normalization.Canonical(kind, other.Field.FieldValue) == canonical)
                {
                    matches.Add(new SharedContact(source.Id, other.CaseId, other.Field.Id, kind));
                }
            }
        }

        return matches.ToArray();
    }
}
