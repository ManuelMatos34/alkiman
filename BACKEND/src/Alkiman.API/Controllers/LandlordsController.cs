using Alkiman.Application.Landlords;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alkiman.API.Controllers;

[ApiController]
[Route("api/landlords")]
[Authorize]
public class LandlordsController : ControllerBase
{
    private readonly ILandlordService _service;

    public LandlordsController(ILandlordService service)
    {
        _service = service;
    }

    /// <summary>Perfil del negocio del usuario autenticado.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<LandlordResponse>> GetMe(CancellationToken cancellationToken)
        => Ok(await _service.GetCurrentAsync(cancellationToken));

    /// <summary>Completa el registro del negocio (primer login tras crear la cuenta en Auth0).</summary>
    [HttpPost("me")]
    public async Task<ActionResult<LandlordResponse>> Register(RegisterLandlordRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.RegisterAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetMe), result);
    }

    [HttpPut("me")]
    public async Task<ActionResult<LandlordResponse>> UpdateMe(UpdateLandlordRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateCurrentAsync(request, cancellationToken));
}
