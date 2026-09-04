using Alkiman.API.Authorization;
using Alkiman.Application.Common.Modules;
using Alkiman.Application.Common.Permissions;
using Alkiman.Application.PortalLinks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alkiman.API.Controllers;

/// <summary>Administración de links del Portal de Rentas (mantenimiento autenticado). Para el catálogo/checkout público que consume el link, ver <see cref="PortalController"/>.</summary>
[ApiController]
[Route("api/portal-links")]
[Authorize]
[RequireModule(ModuleCodes.Alquileres)]
public class PortalLinksController : ControllerBase
{
    private readonly IPortalLinkService _service;

    public PortalLinksController(IPortalLinkService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = PermissionCodes.PortalView)]
    public async Task<ActionResult<IReadOnlyList<PortalLinkResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _service.GetAllAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionCodes.PortalView)]
    public async Task<ActionResult<PortalLinkResponse>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = PermissionCodes.PortalManage)]
    public async Task<ActionResult<PortalLinkResponse>> Create(CreatePortalLinkRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionCodes.PortalManage)]
    public async Task<ActionResult<PortalLinkResponse>> Update(Guid id, UpdatePortalLinkRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PermissionCodes.PortalManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
