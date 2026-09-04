using Alkiman.Application.Common.Permissions;
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

    /// <summary>Perfil del negocio del usuario autenticado. Cualquier usuario autenticado puede consultarlo.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<LandlordResponse>> GetMe(CancellationToken cancellationToken)
        => Ok(await _service.GetCurrentAsync(cancellationToken));

    [HttpPut("me")]
    [Authorize(Policy = PermissionCodes.SettingsManage)]
    public async Task<ActionResult<LandlordResponse>> UpdateMe(UpdateLandlordRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateCurrentAsync(request, cancellationToken));

    [HttpPut("me/appearance")]
    [Authorize(Policy = PermissionCodes.SettingsManage)]
    public async Task<ActionResult<LandlordResponse>> UpdateAppearance(UpdateAppearanceRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateAppearanceAsync(request, cancellationToken));

    [HttpPut("me/signature")]
    [Authorize(Policy = PermissionCodes.SettingsManage)]
    public async Task<ActionResult<LandlordResponse>> UpdateSignature(UpdateLandlordSignatureRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateSignatureAsync(request, cancellationToken));
}
