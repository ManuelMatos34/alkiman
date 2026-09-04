using Alkiman.API.Authorization;
using Alkiman.Application.Common.Modules;
using Alkiman.Application.Common.Permissions;
using Alkiman.Application.RentalRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alkiman.API.Controllers;

/// <summary>
/// Pedidos de prórroga/cancelación que los clientes hacen desde su link público "mi-renta"
/// (ver <see cref="MyRentalController"/>). Acá el negocio los revisa y aprueba/rechaza.
/// </summary>
[ApiController]
[Route("api/rental-requests")]
[Authorize]
[RequireModule(ModuleCodes.Alquileres)]
public class RentalRequestsController : ControllerBase
{
    private readonly IRentalRequestService _service;

    public RentalRequestsController(IRentalRequestService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = PermissionCodes.RentalRequestsView)]
    public async Task<ActionResult<IReadOnlyList<RentalRequestResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _service.GetAllAsync(cancellationToken));

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = PermissionCodes.RentalRequestsManage)]
    public async Task<ActionResult<RentalRequestResponse>> Approve(Guid id, ReviewRentalRequestRequest request, CancellationToken cancellationToken)
        => Ok(await _service.ApproveAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = PermissionCodes.RentalRequestsManage)]
    public async Task<ActionResult<RentalRequestResponse>> Reject(Guid id, ReviewRentalRequestRequest request, CancellationToken cancellationToken)
        => Ok(await _service.RejectAsync(id, request, cancellationToken));
}
