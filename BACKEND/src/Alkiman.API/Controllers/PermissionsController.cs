using Alkiman.Application.Common.Permissions;
using Alkiman.Application.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alkiman.API.Controllers;

/// <summary>Catálogo global de permisos, usado por el editor de roles del frontend.</summary>
[ApiController]
[Route("api/permissions")]
[Authorize(Policy = PermissionCodes.RolesManage)]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _service;

    public PermissionsController(IPermissionService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PermissionResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _service.GetCatalogAsync(cancellationToken));
}
