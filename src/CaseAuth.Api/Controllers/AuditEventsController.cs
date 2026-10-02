using CaseAuth.Api.Contracts;
using CaseAuth.Api.Data;
using CaseAuth.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CaseAuth.Api.Controllers;

[ApiController]
[Route("api/cases/{caseId:guid}/audit-events")]
[Authorize]
public class AuditEventsController(CaseAuthDbContext db, ICaseAccessor caseAccessor) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AuditEventResponse>>> List(Guid caseId, CancellationToken ct)
    {
        await caseAccessor.GetScopedCaseAsync(db, caseId, ct);
        var events = await db.AuditEvents
            .Where(e => e.CaseId == caseId)
            .OrderBy(e => e.Timestamp)
            .ToListAsync(ct);
        return events.Select(AuditEventResponse.From).ToList();
    }
}
