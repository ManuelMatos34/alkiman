using Alkiman.API.Authorization;
using Alkiman.Application.AssetGroups;
using Alkiman.Application.Common.Modules;
using Alkiman.Application.Common.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alkiman.API.Controllers;

/// <summary>Administración de grupos de activos: agrupación libre de activos de cualquier categoría dentro del negocio autenticado.</summary>
[ApiController]
[Route("api/asset-groups")]
[Authorize]
[RequireModule(ModuleCodes.Alquileres)]
public class AssetGroupsController : ControllerBase
{
    private readonly IAssetGroupService _service;

    public AssetGroupsController(IAssetGroupService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = PermissionCodes.AssetGroupsView)]
    public async Task<ActionResult<IReadOnlyList<AssetGroupResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _service.GetAllAsync(cancellationToken));

    [HttpGet("{id:int}")]
    [Authorize(Policy = PermissionCodes.AssetGroupsView)]
    public async Task<ActionResult<AssetGroupResponse>> GetById(int id, CancellationToken cancellationToken)
        => Ok(await _service.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = PermissionCodes.AssetGroupsManage)]
    public async Task<ActionResult<AssetGroupResponse>> Create(CreateAssetGroupRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = PermissionCodes.AssetGroupsManage)]
    public async Task<ActionResult<AssetGroupResponse>> Update(int id, UpdateAssetGroupRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [Authorize(Policy = PermissionCodes.AssetGroupsManage)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
