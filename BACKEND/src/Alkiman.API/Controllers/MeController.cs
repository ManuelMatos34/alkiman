using Alkiman.Application.Me;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alkiman.API.Controllers;

/// <summary>Autoservicio sobre la propia cuenta del usuario autenticado.</summary>
[ApiController]
[Route("api/me")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly IMeService _service;

    public MeController(IMeService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<MeResponse>> Get(CancellationToken cancellationToken)
        => Ok(await _service.GetAsync(cancellationToken));

    [HttpPut]
    public async Task<ActionResult<MeResponse>> Update(UpdateMeRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateAsync(request, cancellationToken));

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword(ChangeMyPasswordRequest request, CancellationToken cancellationToken)
    {
        await _service.ChangePasswordAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPut("two-factor")]
    public async Task<ActionResult<MeResponse>> UpdateTwoFactor(UpdateMyTwoFactorRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateTwoFactorAsync(request, cancellationToken));
}
