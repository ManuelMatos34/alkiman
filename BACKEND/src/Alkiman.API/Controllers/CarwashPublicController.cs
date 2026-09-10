using Alkiman.API.RateLimiting;
using Alkiman.Application.Carwash;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Alkiman.API.Controllers;

/// <summary>
/// Endpoints PÚBLICOS (sin login) del auto-registro de Carwash: el cliente
/// entra con el Slug de un <see cref="Alkiman.Domain.Entities.CarwashPortalLink"/>,
/// ve el catálogo de servicios activos y se une a la cola sin estar
/// físicamente presente. A propósito no lleva [Authorize]: el aislamiento
/// por negocio se resuelve por el Slug del link / el AccessToken del
/// ticket, no por JWT (no hay JWT posible acá; mismo criterio que
/// <see cref="PortalController"/>).
/// </summary>
[ApiController]
[Route("api/carwash/public")]
[EnableRateLimiting(RateLimitPolicies.Public)]
public class CarwashPublicController : ControllerBase
{
    private readonly ICarwashService _service;

    public CarwashPublicController(ICarwashService service)
    {
        _service = service;
    }

    /// <summary>Info del link + catálogo de servicios activos (paso 0 del auto-registro).</summary>
    [HttpGet("{slug}")]
    public async Task<ActionResult<PublicCarwashLinkResponse>> GetLink(string slug, CancellationToken cancellationToken)
        => Ok(await _service.GetPublicLinkAsync(slug, cancellationToken));

    /// <summary>Auto-registro: crea (o reutiliza) al cliente y crea el ticket. Devuelve el AccessToken para navegar a la página de estado.</summary>
    [HttpPost("{slug}/join")]
    public async Task<ActionResult<PublicTicketStatusResponse>> Join(string slug, PublicJoinQueueRequest request, CancellationToken cancellationToken)
        => Ok(await _service.JoinQueueBySlugAsync(slug, request, cancellationToken));

    /// <summary>Estado del turno, para poll desde la página pública.</summary>
    [HttpGet("ticket/{token:guid}")]
    public async Task<ActionResult<PublicTicketStatusResponse>> GetTicketStatus(Guid token, CancellationToken cancellationToken)
        => Ok(await _service.GetTicketByTokenAsync(token, cancellationToken));

    /// <summary>Clave publicable de Stripe. Null si el negocio no cobra en línea: la pasarela omite el paso de pago.</summary>
    [HttpGet("{slug}/payment/config")]
    public async Task<ActionResult<CarwashPublicPaymentConfigResponse>> GetPaymentConfig(string slug, CancellationToken cancellationToken)
        => Ok(await _service.GetPublicPaymentConfigAsync(slug, cancellationToken));

    /// <summary>Inicia el cobro del turno (servicio + extras + propina). El monto lo recalcula el servidor.</summary>
    [HttpPost("{slug}/payment/stripe/intent")]
    public async Task<ActionResult<CarwashStripeIntentResponse>> CreateStripeIntent(
        string slug, CarwashPaymentIntentRequest request, CancellationToken cancellationToken)
        => Ok(await _service.CreatePublicStripeIntentAsync(slug, request, cancellationToken));
}
