using Alkiman.API.Authorization;
using Alkiman.Application.Carwash;
using Alkiman.Application.Common.Modules;
using Alkiman.Application.Common.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alkiman.API.Controllers;

/// <summary>
/// Panel autenticado del módulo Carwash: catálogo de servicios, links de
/// portal público y la cola de vehículos. Lectura con policy
/// <see cref="PermissionCodes.CarwashView"/>, escritura con
/// <see cref="PermissionCodes.CarwashManage"/> (mismo patrón que
/// CategoriesController/RentalRequestsController). Ver <see cref="CarwashPublicController"/>
/// para el flujo público (sin login) del portal.
/// </summary>
[ApiController]
[Route("api/carwash")]
[Authorize]
[RequireModule(ModuleCodes.Carwash)]
public class CarwashController : ControllerBase
{
    private readonly ICarwashService _service;

    public CarwashController(ICarwashService service)
    {
        _service = service;
    }

    // ---- Configuración del módulo ----

    [HttpGet("settings")]
    [Authorize(Policy = PermissionCodes.CarwashView)]
    public async Task<ActionResult<CarwashSettingsResponse>> GetSettings(CancellationToken cancellationToken)
        => Ok(await _service.GetSettingsAsync(cancellationToken));

    [HttpPut("settings")]
    [Authorize(Policy = PermissionCodes.CarwashManage)]
    public async Task<ActionResult<CarwashSettingsResponse>> SaveSettings(SaveCarwashSettingsRequest request, CancellationToken cancellationToken)
        => Ok(await _service.SaveSettingsAsync(request, cancellationToken));

    // ---- Servicios ----

    [HttpGet("services")]
    [Authorize(Policy = PermissionCodes.CarwashView)]
    public async Task<ActionResult<IReadOnlyList<CarwashServiceResponse>>> GetServices(CancellationToken cancellationToken)
        => Ok(await _service.GetServicesAsync(cancellationToken));

    [HttpPost("services")]
    [Authorize(Policy = PermissionCodes.CarwashManage)]
    public async Task<ActionResult<CarwashServiceResponse>> CreateService(CreateServiceRequest request, CancellationToken cancellationToken)
        => Ok(await _service.CreateServiceAsync(request, cancellationToken));

    [HttpPut("services/{id:int}")]
    [Authorize(Policy = PermissionCodes.CarwashManage)]
    public async Task<ActionResult<CarwashServiceResponse>> UpdateService(int id, UpdateServiceRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateServiceAsync(id, request, cancellationToken));

    [HttpDelete("services/{id:int}")]
    [Authorize(Policy = PermissionCodes.CarwashManage)]
    public async Task<IActionResult> DeleteService(int id, CancellationToken cancellationToken)
    {
        await _service.DeleteServiceAsync(id, cancellationToken);
        return NoContent();
    }

    // ---- Extras (agregados que se suman al servicio base) ----

    [HttpGet("extras")]
    [Authorize(Policy = PermissionCodes.CarwashView)]
    public async Task<ActionResult<IReadOnlyList<CarwashExtraResponse>>> GetExtras(CancellationToken cancellationToken)
        => Ok(await _service.GetExtrasAsync(cancellationToken));

    [HttpPost("extras")]
    [Authorize(Policy = PermissionCodes.CarwashManage)]
    public async Task<ActionResult<CarwashExtraResponse>> CreateExtra(CreateExtraRequest request, CancellationToken cancellationToken)
        => Ok(await _service.CreateExtraAsync(request, cancellationToken));

    [HttpPut("extras/{id:int}")]
    [Authorize(Policy = PermissionCodes.CarwashManage)]
    public async Task<ActionResult<CarwashExtraResponse>> UpdateExtra(int id, UpdateExtraRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateExtraAsync(id, request, cancellationToken));

    [HttpDelete("extras/{id:int}")]
    [Authorize(Policy = PermissionCodes.CarwashManage)]
    public async Task<IActionResult> DeleteExtra(int id, CancellationToken cancellationToken)
    {
        await _service.DeleteExtraAsync(id, cancellationToken);
        return NoContent();
    }

    // ---- Links de portal ----

    [HttpGet("portal-links")]
    [Authorize(Policy = PermissionCodes.CarwashView)]
    public async Task<ActionResult<IReadOnlyList<CarwashPortalLinkResponse>>> GetPortalLinks(CancellationToken cancellationToken)
        => Ok(await _service.GetPortalLinksAsync(cancellationToken));

    [HttpPost("portal-links")]
    [Authorize(Policy = PermissionCodes.CarwashManage)]
    public async Task<ActionResult<CarwashPortalLinkResponse>> CreatePortalLink(CreateCarwashPortalLinkRequest request, CancellationToken cancellationToken)
        => Ok(await _service.CreatePortalLinkAsync(request, cancellationToken));

    [HttpPut("portal-links/{id:guid}/active")]
    [Authorize(Policy = PermissionCodes.CarwashManage)]
    public async Task<ActionResult<CarwashPortalLinkResponse>> SetPortalLinkActive(Guid id, [FromBody] bool isActive, CancellationToken cancellationToken)
        => Ok(await _service.SetPortalLinkActiveAsync(id, isActive, cancellationToken));

    // ---- Cola ----

    [HttpGet("queue")]
    [Authorize(Policy = PermissionCodes.CarwashView)]
    public async Task<ActionResult<IReadOnlyList<CarwashTicketResponse>>> GetQueue(CancellationToken cancellationToken)
        => Ok(await _service.GetQueueAsync(cancellationToken));

    [HttpPost("queue/register")]
    [Authorize(Policy = PermissionCodes.CarwashManage)]
    public async Task<ActionResult<CarwashTicketResponse>> RegisterTicket(RegisterTicketRequest request, CancellationToken cancellationToken)
        => Ok(await _service.RegisterTicketAsync(request, cancellationToken));

    // Mover la cola va con CarwashWork y no con CarwashManage: es lo único que
    // necesita hacer el rol Lavador, que no debe poder tocar el catálogo.
    [HttpPut("queue/{id:guid}/status")]
    [Authorize(Policy = PermissionCodes.CarwashWork)]
    public async Task<ActionResult<CarwashTicketResponse>> AdvanceStatus(Guid id, AdvanceStatusRequest request, CancellationToken cancellationToken)
        => Ok(await _service.AdvanceStatusAsync(id, request, cancellationToken));

    // ---- Directorio de lavadores ----
    // Entidad propia del módulo, no usuarios del sistema: ver CarwashWasher.
    // Leer alcanza con CarwashView porque el tablero necesita la lista para
    // asignar; administrar el plantel pide CarwashManage.

    [HttpGet("washers")]
    [Authorize(Policy = PermissionCodes.CarwashView)]
    public async Task<ActionResult<IReadOnlyList<CarwashWasherResponse>>> GetWashers(CancellationToken cancellationToken)
        => Ok(await _service.GetWashersAsync(cancellationToken));

    [HttpPost("washers")]
    [Authorize(Policy = PermissionCodes.CarwashManage)]
    public async Task<ActionResult<CarwashWasherResponse>> CreateWasher(CreateWasherRequest request, CancellationToken cancellationToken)
        => Ok(await _service.CreateWasherAsync(request, cancellationToken));

    [HttpPut("washers/{id:guid}")]
    [Authorize(Policy = PermissionCodes.CarwashManage)]
    public async Task<ActionResult<CarwashWasherResponse>> UpdateWasher(Guid id, UpdateWasherRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateWasherAsync(id, request, cancellationToken));

    [HttpDelete("washers/{id:guid}")]
    [Authorize(Policy = PermissionCodes.CarwashManage)]
    public async Task<IActionResult> DeleteWasher(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteWasherAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Cuentas del negocio que se le pueden vincular a un lavador. El vínculo es opcional: ver CarwashWasher.UserId.</summary>
    [HttpGet("washers/linkable-users")]
    [Authorize(Policy = PermissionCodes.CarwashManage)]
    public async Task<ActionResult<IReadOnlyList<CarwashLinkableUserResponse>>> GetLinkableUsers([FromQuery] Guid? washerId, CancellationToken cancellationToken)
        => Ok(await _service.GetLinkableUsersAsync(washerId, cancellationToken));

    [HttpPut("queue/{id:guid}/assign")]
    [Authorize(Policy = PermissionCodes.CarwashManage)]
    public async Task<ActionResult<CarwashTicketResponse>> AssignWasher(Guid id, AssignWasherRequest request, CancellationToken cancellationToken)
        => Ok(await _service.AssignWasherAsync(id, request, cancellationToken));

    [HttpPost("queue/{id:guid}/cancel")]
    [Authorize(Policy = PermissionCodes.CarwashManage)]
    public async Task<ActionResult<CarwashTicketResponse>> CancelTicket(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.CancelTicketAsync(id, cancellationToken));

    [HttpPost("queue/{id:guid}/expire")]
    [Authorize(Policy = PermissionCodes.CarwashManage)]
    public async Task<ActionResult<CarwashTicketResponse>> MarkExpired(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.MarkExpiredAsync(id, cancellationToken));

    [HttpPost("queue/{id:guid}/call-arrival")]
    [Authorize(Policy = PermissionCodes.CarwashManage)]
    public async Task<ActionResult<CarwashTicketResponse>> CallArrival(Guid id, [FromBody] int? deadlineMinutes, CancellationToken cancellationToken)
        => Ok(await _service.CallArrivalAsync(id, deadlineMinutes, cancellationToken));

    [HttpPost("queue/{id:guid}/confirm-arrival")]
    [Authorize(Policy = PermissionCodes.CarwashManage)]
    public async Task<ActionResult<CarwashTicketResponse>> ConfirmArrival(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.ConfirmArrivalAsync(id, cancellationToken));
}
