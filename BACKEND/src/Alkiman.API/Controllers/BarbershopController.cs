using Alkiman.API.Authorization;
using Alkiman.Application.Barbershop;
using Alkiman.Application.Common.Modules;
using Alkiman.Application.Common.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alkiman.API.Controllers;

/// <summary>
/// Panel autenticado del módulo Barbería: catálogo de servicios, estilistas,
/// links de portal público y el tablero de citas.
/// Ver <see cref="BarbershopPublicController"/> para el flujo público sin login.
/// </summary>
[ApiController]
[Route("api/barbershop")]
[Authorize]
[RequireModule(ModuleCodes.Barbershop)]
public class BarbershopController : ControllerBase
{
    private readonly IBarbershopService _service;

    public BarbershopController(IBarbershopService service)
    {
        _service = service;
    }

    // ---- Métricas ----

    [HttpGet("metrics")]
    [Authorize(Policy = PermissionCodes.BarbershopReports)]
    public async Task<ActionResult<BarbershopMetricsResponse>> GetMetrics(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
        => Ok(await _service.GetMetricsAsync(from, to, cancellationToken));

    // ---- Servicios (catálogo) ----

    [HttpGet("services")]
    [Authorize(Policy = PermissionCodes.BarbershopCatalogView)]
    public async Task<ActionResult<IReadOnlyList<BarbershopServiceResponse>>> GetServices(CancellationToken cancellationToken)
        => Ok(await _service.GetServicesAsync(cancellationToken));

    [HttpPost("services")]
    [Authorize(Policy = PermissionCodes.BarbershopCatalogManage)]
    public async Task<ActionResult<BarbershopServiceResponse>> CreateService(CreateBarbershopServiceRequest request, CancellationToken cancellationToken)
        => Ok(await _service.CreateServiceAsync(request, cancellationToken));

    [HttpPut("services/{id:int}")]
    [Authorize(Policy = PermissionCodes.BarbershopCatalogManage)]
    public async Task<ActionResult<BarbershopServiceResponse>> UpdateService(int id, UpdateBarbershopServiceRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateServiceAsync(id, request, cancellationToken));

    [HttpDelete("services/{id:int}")]
    [Authorize(Policy = PermissionCodes.BarbershopCatalogManage)]
    public async Task<IActionResult> DeleteService(int id, CancellationToken cancellationToken)
    {
        await _service.DeleteServiceAsync(id, cancellationToken);
        return NoContent();
    }

    // ---- Estilistas ----

    [HttpGet("stylists")]
    [Authorize(Policy = PermissionCodes.BarbershopStylistsView)]
    public async Task<ActionResult<IReadOnlyList<BarbershopStylistResponse>>> GetStylists(CancellationToken cancellationToken)
        => Ok(await _service.GetStylistsAsync(cancellationToken));

    [HttpPost("stylists")]
    [Authorize(Policy = PermissionCodes.BarbershopStylistsManage)]
    public async Task<ActionResult<BarbershopStylistResponse>> CreateStylist(CreateBarbershopStylistRequest request, CancellationToken cancellationToken)
        => Ok(await _service.CreateStylistAsync(request, cancellationToken));

    [HttpPut("stylists/{id:guid}")]
    [Authorize(Policy = PermissionCodes.BarbershopStylistsManage)]
    public async Task<ActionResult<BarbershopStylistResponse>> UpdateStylist(Guid id, UpdateBarbershopStylistRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateStylistAsync(id, request, cancellationToken));

    [HttpDelete("stylists/{id:guid}")]
    [Authorize(Policy = PermissionCodes.BarbershopStylistsManage)]
    public async Task<IActionResult> DeleteStylist(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteStylistAsync(id, cancellationToken);
        return NoContent();
    }

    // ---- Links de portal ----

    [HttpGet("portal-links")]
    [Authorize(Policy = PermissionCodes.BarbershopPortalView)]
    public async Task<ActionResult<IReadOnlyList<BarbershopPortalLinkResponse>>> GetPortalLinks(CancellationToken cancellationToken)
        => Ok(await _service.GetPortalLinksAsync(cancellationToken));

    [HttpPost("portal-links")]
    [Authorize(Policy = PermissionCodes.BarbershopPortalManage)]
    public async Task<ActionResult<BarbershopPortalLinkResponse>> CreatePortalLink(CreateBarbershopPortalLinkRequest request, CancellationToken cancellationToken)
        => Ok(await _service.CreatePortalLinkAsync(request, cancellationToken));

    [HttpPut("portal-links/{id:guid}/active")]
    [Authorize(Policy = PermissionCodes.BarbershopPortalManage)]
    public async Task<ActionResult<BarbershopPortalLinkResponse>> SetPortalLinkActive(Guid id, [FromBody] SetBarbershopPortalLinkActiveRequest request, CancellationToken cancellationToken)
        => Ok(await _service.SetPortalLinkActiveAsync(id, request.IsActive, cancellationToken));

    [HttpDelete("portal-links/{id:guid}")]
    [Authorize(Policy = PermissionCodes.BarbershopPortalManage)]
    public async Task<IActionResult> DeletePortalLink(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeletePortalLinkAsync(id, cancellationToken);
        return NoContent();
    }

    // ---- Slots de disponibilidad ----

    [HttpGet("slots")]
    [Authorize(Policy = PermissionCodes.BarbershopBoardManage)]
    public async Task<ActionResult<IReadOnlyList<BarbershopSlotResponse>>> GetSlots(
        [FromQuery] DateTime date,
        [FromQuery] int? serviceId,
        CancellationToken cancellationToken)
        => Ok(await _service.GetSlotsAsync(date, serviceId, cancellationToken));

    // ---- Tablero / Citas ----

    [HttpGet("appointments")]
    [Authorize(Policy = PermissionCodes.BarbershopBoardView)]
    public async Task<ActionResult<IReadOnlyList<BarbershopAppointmentResponse>>> GetAppointments(
        [FromQuery] DateTime? date,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
        => Ok(await _service.GetAppointmentsAsync(date, from, to, cancellationToken));

    [HttpPost("appointments")]
    [Authorize(Policy = PermissionCodes.BarbershopBoardManage)]
    public async Task<ActionResult<BarbershopAppointmentResponse>> CreateAppointment(CreateBarbershopAppointmentRequest request, CancellationToken cancellationToken)
        => Ok(await _service.CreateAppointmentAsync(request, cancellationToken));

    [HttpPut("appointments/{id:guid}/status")]
    [Authorize(Policy = PermissionCodes.BarbershopBoardWork)]
    public async Task<ActionResult<BarbershopAppointmentResponse>> AdvanceStatus(
        Guid id, [FromBody] AdvanceBarbershopStatusRequest request, CancellationToken cancellationToken)
        => Ok(await _service.AdvanceStatusAsync(id, request.IsPaid, cancellationToken));

    [HttpPut("appointments/{id:guid}/pay")]
    [Authorize(Policy = PermissionCodes.BarbershopBoardWork)]
    public async Task<ActionResult<BarbershopAppointmentResponse>> MarkAsPaid(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.MarkAsPaidAsync(id, cancellationToken));

    [HttpPost("appointments/{id:guid}/cancel")]
    [Authorize(Policy = PermissionCodes.BarbershopBoardManage)]
    public async Task<IActionResult> CancelAppointment(Guid id, CancellationToken cancellationToken)
    {
        await _service.CancelAppointmentAsync(id, cancellationToken);
        return NoContent();
    }
}
