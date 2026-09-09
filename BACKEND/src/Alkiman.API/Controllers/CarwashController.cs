using Alkiman.API.Authorization;
using Alkiman.Application.Carwash;
using Alkiman.Application.Common.Modules;
using Alkiman.Application.Common.Permissions;
using Alkiman.Domain.Entities;
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
    private readonly ICarwashMetricsService _metrics;

    public CarwashController(ICarwashService service, ICarwashMetricsService metrics)
    {
        _service = service;
        _metrics = metrics;
    }

    // ---- Métricas ----

    /// <summary>
    /// Tablero del módulo: volumen, facturación, servicios más usados, distribución
    /// horaria y ranking de lavadores. Rango inclusivo; sin fechas, los últimos 30 días.
    /// </summary>
    [HttpGet("metrics")]
    [Authorize(Policy = PermissionCodes.CarwashReports)]
    public async Task<ActionResult<CarwashMetricsResponse>> GetMetrics(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
        => Ok(await _metrics.GetMetricsAsync(from, to, cancellationToken));

    // ---- Configuración del módulo ----

    [HttpGet("settings")]
    [Authorize(Policy = PermissionCodes.CarwashBoardView)]
    public async Task<ActionResult<CarwashSettingsResponse>> GetSettings(CancellationToken cancellationToken)
        => Ok(await _service.GetSettingsAsync(cancellationToken));

    [HttpPut("settings")]
    [Authorize(Policy = PermissionCodes.CarwashBoardManage)]
    public async Task<ActionResult<CarwashSettingsResponse>> SaveSettings(SaveCarwashSettingsRequest request, CancellationToken cancellationToken)
        => Ok(await _service.SaveSettingsAsync(request, cancellationToken));

    // ---- Servicios ----

    [HttpGet("services")]
    [Authorize(Policy = PermissionCodes.CarwashCatalogView)]
    public async Task<ActionResult<IReadOnlyList<CarwashServiceResponse>>> GetServices(CancellationToken cancellationToken)
        => Ok(await _service.GetServicesAsync(cancellationToken));

    [HttpPost("services")]
    [Authorize(Policy = PermissionCodes.CarwashCatalogManage)]
    public async Task<ActionResult<CarwashServiceResponse>> CreateService(CreateServiceRequest request, CancellationToken cancellationToken)
        => Ok(await _service.CreateServiceAsync(request, cancellationToken));

    [HttpPut("services/{id:int}")]
    [Authorize(Policy = PermissionCodes.CarwashCatalogManage)]
    public async Task<ActionResult<CarwashServiceResponse>> UpdateService(int id, UpdateServiceRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateServiceAsync(id, request, cancellationToken));

    [HttpDelete("services/{id:int}")]
    [Authorize(Policy = PermissionCodes.CarwashCatalogManage)]
    public async Task<IActionResult> DeleteService(int id, CancellationToken cancellationToken)
    {
        await _service.DeleteServiceAsync(id, cancellationToken);
        return NoContent();
    }

    // ---- Extras (agregados que se suman al servicio base) ----

    [HttpGet("extras")]
    [Authorize(Policy = PermissionCodes.CarwashCatalogView)]
    public async Task<ActionResult<IReadOnlyList<CarwashExtraResponse>>> GetExtras(CancellationToken cancellationToken)
        => Ok(await _service.GetExtrasAsync(cancellationToken));

    [HttpPost("extras")]
    [Authorize(Policy = PermissionCodes.CarwashCatalogManage)]
    public async Task<ActionResult<CarwashExtraResponse>> CreateExtra(CreateExtraRequest request, CancellationToken cancellationToken)
        => Ok(await _service.CreateExtraAsync(request, cancellationToken));

    [HttpPut("extras/{id:int}")]
    [Authorize(Policy = PermissionCodes.CarwashCatalogManage)]
    public async Task<ActionResult<CarwashExtraResponse>> UpdateExtra(int id, UpdateExtraRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateExtraAsync(id, request, cancellationToken));

    [HttpDelete("extras/{id:int}")]
    [Authorize(Policy = PermissionCodes.CarwashCatalogManage)]
    public async Task<IActionResult> DeleteExtra(int id, CancellationToken cancellationToken)
    {
        await _service.DeleteExtraAsync(id, cancellationToken);
        return NoContent();
    }

    // ---- Links de portal ----

    [HttpGet("portal-links")]
    [Authorize(Policy = PermissionCodes.CarwashPortalView)]
    public async Task<ActionResult<IReadOnlyList<CarwashPortalLinkResponse>>> GetPortalLinks(CancellationToken cancellationToken)
        => Ok(await _service.GetPortalLinksAsync(cancellationToken));

    [HttpPost("portal-links")]
    [Authorize(Policy = PermissionCodes.CarwashPortalManage)]
    public async Task<ActionResult<CarwashPortalLinkResponse>> CreatePortalLink(CreateCarwashPortalLinkRequest request, CancellationToken cancellationToken)
        => Ok(await _service.CreatePortalLinkAsync(request, cancellationToken));

    [HttpPut("portal-links/{id:guid}/active")]
    [Authorize(Policy = PermissionCodes.CarwashPortalManage)]
    public async Task<ActionResult<CarwashPortalLinkResponse>> SetPortalLinkActive(Guid id, [FromBody] SetPortalLinkActiveRequest request, CancellationToken cancellationToken)
        => Ok(await _service.SetPortalLinkActiveAsync(id, request.IsActive, cancellationToken));

    [HttpDelete("portal-links/{id:guid}")]
    [Authorize(Policy = PermissionCodes.CarwashPortalManage)]
    public async Task<IActionResult> DeletePortalLink(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeletePortalLinkAsync(id, cancellationToken);
        return NoContent();
    }

    // ---- Cola ----

    [HttpGet("queue")]
    [Authorize(Policy = PermissionCodes.CarwashBoardView)]
    public async Task<ActionResult<IReadOnlyList<CarwashTicketResponse>>> GetQueue(CancellationToken cancellationToken)
        => Ok(await _service.GetQueueAsync(cancellationToken));

    [HttpPost("queue/register")]
    [Authorize(Policy = PermissionCodes.CarwashBoardManage)]
    public async Task<ActionResult<CarwashTicketResponse>> RegisterTicket(RegisterTicketRequest request, CancellationToken cancellationToken)
        => Ok(await _service.RegisterTicketAsync(request, cancellationToken));

    [HttpPut("queue/{id:guid}/status")]
    [Authorize(Policy = PermissionCodes.CarwashBoardWork)]
    public async Task<ActionResult<CarwashTicketResponse>> AdvanceStatus(Guid id, [FromBody] AdvanceStatusRequest request, CancellationToken cancellationToken)
        => Ok(await _service.AdvanceStatusAsync(id, request, cancellationToken));

    // ---- Directorio de lavadores ----
    // GET alcanza con CarwashBoardView porque el tablero necesita la lista
    // para mostrar el picker de asignación.

    [HttpGet("washers")]
    [Authorize(Policy = PermissionCodes.CarwashBoardView)]
    public async Task<ActionResult<IReadOnlyList<CarwashWasherResponse>>> GetWashers(CancellationToken cancellationToken)
        => Ok(await _service.GetWashersAsync(cancellationToken));

    [HttpPost("washers")]
    [Authorize(Policy = PermissionCodes.CarwashWashersManage)]
    public async Task<ActionResult<CarwashWasherResponse>> CreateWasher(CreateWasherRequest request, CancellationToken cancellationToken)
        => Ok(await _service.CreateWasherAsync(request, cancellationToken));

    [HttpPut("washers/{id:guid}")]
    [Authorize(Policy = PermissionCodes.CarwashWashersManage)]
    public async Task<ActionResult<CarwashWasherResponse>> UpdateWasher(Guid id, UpdateWasherRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateWasherAsync(id, request, cancellationToken));

    [HttpDelete("washers/{id:guid}")]
    [Authorize(Policy = PermissionCodes.CarwashWashersManage)]
    public async Task<IActionResult> DeleteWasher(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteWasherAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Endpoint exclusivo de Caja: marca el ticket como Entregado y registra la propina.
    /// Requiere <see cref="PermissionCodes.CarwashCaja"/>; la Cajera no necesita
    /// <see cref="PermissionCodes.CarwashBoardWork"/> ni <see cref="PermissionCodes.CarwashBoardManage"/>.
    /// </summary>
    [HttpPost("queue/{id:guid}/deliver")]
    [Authorize(Policy = PermissionCodes.CarwashCaja)]
    public async Task<ActionResult<CarwashTicketResponse>> DeliverTicket(
        Guid id, [FromBody] DeliverTicketRequest request, CancellationToken cancellationToken)
        => Ok(await _service.AdvanceStatusAsync(
            id,
            new AdvanceStatusRequest(CarwashTicketStatus.Delivered, request.TipAmount),
            cancellationToken));

    /// <summary>Retrocede el estado de un ticket al anterior. Útil cuando el encargado avanzó por error.</summary>
    [HttpPost("queue/{id:guid}/go-back")]
    [Authorize(Policy = PermissionCodes.CarwashBoardWork)]
    public async Task<ActionResult<CarwashTicketResponse>> GoBack(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.GoBackStatusAsync(id, cancellationToken));

    [HttpPut("queue/{id:guid}/assign")]
    [Authorize(Policy = PermissionCodes.CarwashBoardManage)]
    public async Task<ActionResult<CarwashTicketResponse>> AssignWasher(Guid id, AssignWasherRequest request, CancellationToken cancellationToken)
        => Ok(await _service.AssignWasherAsync(id, request, cancellationToken));

    [HttpPost("queue/{id:guid}/cancel")]
    [Authorize(Policy = PermissionCodes.CarwashBoardManage)]
    public async Task<ActionResult<CarwashTicketResponse>> CancelTicket(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.CancelTicketAsync(id, cancellationToken));

    [HttpPost("queue/{id:guid}/expire")]
    [Authorize(Policy = PermissionCodes.CarwashBoardManage)]
    public async Task<ActionResult<CarwashTicketResponse>> MarkExpired(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.MarkExpiredAsync(id, cancellationToken));

    [HttpPost("queue/{id:guid}/call-arrival")]
    [Authorize(Policy = PermissionCodes.CarwashBoardManage)]
    public async Task<ActionResult<CarwashTicketResponse>> CallArrival(Guid id, [FromBody] int? deadlineMinutes, CancellationToken cancellationToken)
        => Ok(await _service.CallArrivalAsync(id, deadlineMinutes, cancellationToken));

    [HttpPost("queue/{id:guid}/confirm-arrival")]
    [Authorize(Policy = PermissionCodes.CarwashBoardManage)]
    public async Task<ActionResult<CarwashTicketResponse>> ConfirmArrival(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.ConfirmArrivalAsync(id, cancellationToken));
}
