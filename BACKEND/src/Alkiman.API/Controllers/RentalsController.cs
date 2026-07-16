using Alkiman.Application.Rentals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alkiman.API.Controllers;

[ApiController]
[Route("api/rentals")]
[Authorize]
public class RentalsController : ControllerBase
{
    private readonly IRentalService _service;

    public RentalsController(IRentalService service)
    {
        _service = service;
    }

    /// <summary>Todas las rentas del negocio (alimenta el tablero de calendario).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RentalResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _service.GetAllAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RentalResponse>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.GetByIdAsync(id, cancellationToken));

    /// <summary>Asigna una renta a un activo. El activo pasa automáticamente a estado "Rentado".</summary>
    [HttpPost]
    public async Task<ActionResult<RentalResponse>> Create(CreateRentalRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Adjunta la URL del PDF del contrato ya firmado digitalmente.</summary>
    [HttpPatch("{id:guid}/contract")]
    public async Task<ActionResult<RentalResponse>> AttachContract(Guid id, UpdateRentalContractRequest request, CancellationToken cancellationToken)
        => Ok(await _service.AttachContractAsync(id, request, cancellationToken));

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<RentalResponse>> UpdateStatus(Guid id, UpdateRentalStatusRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateStatusAsync(id, request, cancellationToken));
}
