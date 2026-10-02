using CaseAuth.Api.Auth;
using CaseAuth.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CaseAuth.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public class MeController(ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public ActionResult<MeResponse> Get() =>
        new MeResponse(currentUser.UserId, currentUser.Username, currentUser.FirmId, currentUser.Role);
}
