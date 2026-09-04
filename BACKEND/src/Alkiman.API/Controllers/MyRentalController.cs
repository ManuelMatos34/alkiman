using Alkiman.Application.MyRental;
using Microsoft.AspNetCore.Mvc;

namespace Alkiman.API.Controllers;

/// <summary>
/// Endpoints PÚBLICOS (sin login) del link "mi-renta" (/mi-renta/{token}): el cliente
/// verifica su identidad y puede pedir una prórroga o una cancelación de su propia renta.
/// A propósito no lleva [Authorize] en ningún lado: el aislamiento se resuelve por el
/// AccessToken de la URL + la verificación de identidad dentro del servicio, no por JWT
/// (no hay JWT posible acá, el cliente no está logueado; mismo criterio que PortalController).
/// </summary>
[ApiController]
[Route("api/my-rental")]
public class MyRentalController : ControllerBase
{
    private readonly IMyRentalService _service;

    public MyRentalController(IMyRentalService service)
    {
        _service = service;
    }

    [HttpPost("{token:guid}/verify")]
    public async Task<ActionResult<MyRentalResponse>> Verify(Guid token, VerifyMyRentalRequest request, CancellationToken cancellationToken)
        => Ok(await _service.VerifyAsync(token, request, cancellationToken));

    [HttpPost("{token:guid}/extension-requests")]
    public async Task<IActionResult> RequestExtension(Guid token, CreateMyRentalExtensionRequest request, CancellationToken cancellationToken)
    {
        await _service.RequestExtensionAsync(token, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("{token:guid}/cancellation-requests")]
    public async Task<IActionResult> RequestCancellation(Guid token, CreateMyRentalCancellationRequest request, CancellationToken cancellationToken)
    {
        await _service.RequestCancellationAsync(token, request, cancellationToken);
        return NoContent();
    }
}
