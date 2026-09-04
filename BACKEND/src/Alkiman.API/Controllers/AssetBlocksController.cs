using Alkiman.API.Authorization;
using Alkiman.Application.AssetBlocks;
using Alkiman.Application.Common.Modules;
using Alkiman.Application.Common.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alkiman.API.Controllers;

[ApiController]
[Route("api/asset-blocks")]
[Authorize]
[RequireModule(ModuleCodes.Alquileres)]
public class AssetBlocksController : ControllerBase
{
    private readonly IAssetBlockService _service;

    public AssetBlocksController(IAssetBlockService service)
    {
        _service = service;
    }

    /// <summary>Bloqueos de fecha de un activo (mantenimiento, reparación, etc. — RF-B2).</summary>
    [HttpGet]
    [Authorize(Policy = PermissionCodes.AssetsView)]
    public async Task<ActionResult<IReadOnlyList<AssetBlockResponse>>> GetByAsset([FromQuery] Guid assetId, CancellationToken cancellationToken)
        => Ok(await _service.GetAllByAssetAsync(assetId, cancellationToken));

    [HttpPost]
    [Authorize(Policy = PermissionCodes.AssetsManage)]
    public async Task<ActionResult<AssetBlockResponse>> Create(CreateAssetBlockRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByAsset), new { assetId = result.AssetId }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionCodes.AssetsManage)]
    public async Task<ActionResult<AssetBlockResponse>> Update(Guid id, UpdateAssetBlockRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PermissionCodes.AssetsManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
