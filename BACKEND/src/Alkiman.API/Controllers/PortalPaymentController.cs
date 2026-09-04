using Alkiman.Application.Portal;
using Microsoft.AspNetCore.Mvc;

namespace Alkiman.API.Controllers;

/// <summary>
/// Endpoints PÚBLICOS (sin login) de la pasarela de pago sandbox (Stripe) del Portal de
/// Rentas. Mismo criterio que <see cref="PortalController"/>: sin [Authorize], el aislamiento por
/// negocio se resuelve por el Slug del link.
/// </summary>
[ApiController]
[Route("api/portal/{slug}/payment")]
public class PortalPaymentController : ControllerBase
{
    private readonly IPortalPaymentService _service;

    public PortalPaymentController(IPortalPaymentService service)
    {
        _service = service;
    }

    /// <summary>Claves públicas de Stripe (vacías si el proveedor todavía no está configurado) para inicializar el checkout.</summary>
    [HttpGet("config")]
    public async Task<ActionResult<PortalPaymentConfigResponse>> GetConfig(string slug, CancellationToken cancellationToken)
        => Ok(await _service.GetConfigAsync(slug, cancellationToken));

    /// <summary>Crea un Stripe PaymentIntent por el precio recalculado server-side.</summary>
    [HttpPost("stripe/intent")]
    public async Task<ActionResult<StripeIntentResponse>> CreateStripeIntent(string slug, PortalPaymentStartRequest request, CancellationToken cancellationToken)
        => Ok(await _service.CreateStripeIntentAsync(slug, request, cancellationToken));
}
