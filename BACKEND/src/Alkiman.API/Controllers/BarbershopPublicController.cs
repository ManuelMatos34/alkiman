using Alkiman.API.RateLimiting;
using Alkiman.Application.Barbershop;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Alkiman.API.Controllers;

/// <summary>
/// Endpoints PÚBLICOS (sin login) del módulo Barbería: el cliente
/// entra con el Slug de un <see cref="Alkiman.Domain.Entities.BarbershopPortalLink"/>,
/// ve los servicios disponibles y agenda una cita sin estar registrado.
/// A propósito no lleva [Authorize]: el aislamiento por negocio se resuelve por
/// el Slug / TrackingToken, no por JWT (no hay JWT posible acá).
/// </summary>
[ApiController]
[Route("api/barbershop/public")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicies.Public)]
public class BarbershopPublicController : ControllerBase
{
    private readonly IBarbershopService _service;

    public BarbershopPublicController(IBarbershopService service)
    {
        _service = service;
    }

    /// <summary>Info del link + catálogo de servicios activos (paso 0 del agendamiento).</summary>
    [HttpGet("{slug}")]
    public async Task<ActionResult<BarbershopPublicLinkResponse>> GetLink(string slug, CancellationToken cancellationToken)
        => Ok(await _service.GetPublicLinkAsync(slug, cancellationToken));

    /// <summary>Agendamiento: crea la cita y devuelve el TrackingToken para ver el estado.</summary>
    [HttpPost("{slug}/book")]
    public async Task<ActionResult<BarbershopBookingResponse>> Book(string slug, BookBarbershopAppointmentRequest request, CancellationToken cancellationToken)
        => Ok(await _service.BookBySlugAsync(slug, request, cancellationToken));

    /// <summary>Slots disponibles para el portal público (sin login).</summary>
    [HttpGet("{slug}/slots")]
    public async Task<ActionResult<IReadOnlyList<BarbershopSlotResponse>>> GetSlots(
        string slug,
        [FromQuery] DateTime date,
        [FromQuery] int? serviceId,
        CancellationToken cancellationToken)
        => Ok(await _service.GetPublicSlotsAsync(slug, date, serviceId, cancellationToken));

    /// <summary>Estado de la cita, para consulta pública por tracking token.</summary>
    [HttpGet("appointment/{token}")]
    public async Task<ActionResult<BarbershopAppointmentStatusResponse>> GetAppointmentStatus(string token, CancellationToken cancellationToken)
        => Ok(await _service.GetAppointmentByTokenAsync(token, cancellationToken));
}
