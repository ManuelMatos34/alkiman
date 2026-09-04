using Alkiman.Application.Portal;
using Microsoft.AspNetCore.Mvc;

namespace Alkiman.API.Controllers;

/// <summary>
/// Endpoints PÚBLICOS (sin login) del Portal de Rentas: el catálogo y el checkout que
/// consume el cliente que recibe un link generado desde <see cref="PortalLinksController"/>.
/// A propósito no lleva [Authorize]: el aislamiento por negocio se resuelve por el Slug
/// del link, no por JWT (no hay JWT posible acá, el visitante no está logueado).
/// </summary>
[ApiController]
[Route("api/portal")]
public class PortalController : ControllerBase
{
    private readonly IPortalService _service;

    public PortalController(IPortalService service)
    {
        _service = service;
    }

    /// <summary>Catálogo público de activos del grupo asociado al link (paso 0: elegir activo).</summary>
    [HttpGet("{slug}")]
    public async Task<ActionResult<PortalCatalogResponse>> GetCatalog(string slug, CancellationToken cancellationToken)
        => Ok(await _service.GetCatalogAsync(slug, cancellationToken));

    /// <summary>Submit final de la pasarela de 3 pasos: crea (o reutiliza) al cliente y crea la renta.</summary>
    [HttpPost("{slug}/checkout")]
    public async Task<ActionResult<PortalCheckoutResponse>> Checkout(string slug, PortalCheckoutRequest request, CancellationToken cancellationToken)
        => Ok(await _service.CheckoutAsync(slug, request, cancellationToken));

    /// <summary>Vista previa del contrato (paso 3 de la pasarela, antes de firmar y pagar): no persiste nada.</summary>
    [HttpPost("{slug}/contract-preview")]
    public async Task<ActionResult<PortalContractPreviewResponse>> PreviewContract(string slug, PortalContractPreviewRequest request, CancellationToken cancellationToken)
        => Ok(await _service.PreviewContractAsync(slug, request, cancellationToken));
}
