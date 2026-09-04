using Alkiman.API.Authorization;
using Alkiman.Application.Common.Modules;
using Alkiman.Application.Common.Permissions;
using Alkiman.Application.Rentals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alkiman.API.Controllers;

[ApiController]
[Route("api/rentals")]
[Authorize]
[RequireModule(ModuleCodes.Alquileres)]
public class RentalsController : ControllerBase
{
    private readonly IRentalService _service;

    public RentalsController(IRentalService service)
    {
        _service = service;
    }

    /// <summary>Todas las rentas del negocio (alimenta el tablero de calendario).</summary>
    [HttpGet]
    [Authorize(Policy = PermissionCodes.RentalsView)]
    public async Task<ActionResult<IReadOnlyList<RentalResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _service.GetAllAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionCodes.RentalsView)]
    public async Task<ActionResult<RentalResponse>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.GetByIdAsync(id, cancellationToken));

    /// <summary>Asigna una renta a un activo. El activo pasa automáticamente a estado "Rentado".</summary>
    [HttpPost]
    [Authorize(Policy = PermissionCodes.RentalsManage)]
    public async Task<ActionResult<RentalResponse>> Create(CreateRentalRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Adjunta la URL del PDF del contrato ya firmado digitalmente.</summary>
    [HttpPatch("{id:guid}/contract")]
    [Authorize(Policy = PermissionCodes.RentalsManage)]
    public async Task<ActionResult<RentalResponse>> AttachContract(Guid id, UpdateRentalContractRequest request, CancellationToken cancellationToken)
        => Ok(await _service.AttachContractAsync(id, request, cancellationToken));

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = PermissionCodes.RentalsManage)]
    public async Task<ActionResult<RentalResponse>> UpdateStatus(Guid id, UpdateRentalStatusRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateStatusAsync(id, request, cancellationToken));
}
