using Alkiman.API.Authorization;
using Alkiman.Application.Assets;
using Alkiman.Application.Common.Modules;
using Alkiman.Application.Common.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alkiman.API.Controllers;

[ApiController]
[Route("api/assets")]
[Authorize]
[RequireModule(ModuleCodes.Alquileres)]
public class AssetsController : ControllerBase
{
    private readonly IAssetService _service;

    public AssetsController(IAssetService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = PermissionCodes.AssetsView)]
    public async Task<ActionResult<IReadOnlyList<AssetResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _service.GetAllAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionCodes.AssetsView)]
    public async Task<ActionResult<AssetResponse>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = PermissionCodes.AssetsManage)]
    public async Task<ActionResult<AssetResponse>> Create(CreateAssetRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionCodes.AssetsManage)]
    public async Task<ActionResult<AssetResponse>> Update(Guid id, UpdateAssetRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateAsync(id, request, cancellationToken));

    /// <summary>Cambia el estado del activo (Disponible / Rentado / Mantenimiento).</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = PermissionCodes.AssetsManage)]
    public async Task<ActionResult<AssetResponse>> UpdateStatus(Guid id, UpdateAssetStatusRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateStatusAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PermissionCodes.AssetsManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
