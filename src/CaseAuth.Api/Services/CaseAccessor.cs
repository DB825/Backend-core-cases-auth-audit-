using CaseAuth.Api.Auth;
using CaseAuth.Api.Data;
using CaseAuth.Api.Entities;
using CaseAuth.Api.Errors;
using Microsoft.EntityFrameworkCore;

namespace CaseAuth.Api.Services;

public interface ICaseAccessor
{
    // Every read or mutation of a Case must go through here: it is the one place that
    // enforces firm scoping. A case that exists but belongs to another firm 404s exactly
    // like one that doesn't exist, so callers cannot probe for cross-firm case ids.
    Task<Case> GetScopedCaseAsync(CaseAuthDbContext db, Guid caseId, CancellationToken ct, bool includeApplicant = false);
}

public class CaseAccessor(ICurrentUser currentUser) : ICaseAccessor
{
    public async Task<Case> GetScopedCaseAsync(CaseAuthDbContext db, Guid caseId, CancellationToken ct, bool includeApplicant = false)
    {
        var query = db.Cases.AsQueryable();
        if (includeApplicant)
        {
            query = query.Include(c => c.Applicant);
        }

        var c = await query.FirstOrDefaultAsync(c => c.Id == caseId, ct);
        if (c is null || c.FirmId != currentUser.FirmId)
        {
            throw new NotFoundApiException($"Case '{caseId}' was not found.");
        }

        return c;
    }
}
