using Alkiman.API.Authorization;
using Alkiman.Application.Common.Modules;
using Alkiman.Application.Common.Permissions;
using Alkiman.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alkiman.API.Controllers;

/// <summary>
/// Contratos ya generados (uno por renta, automáticamente al crearse). Acá el negocio
/// puede ver el detalle, descargar/visualizar el PDF y registrar la firma de las rentas
/// creadas manualmente (las del Portal ya nacen firmadas por el cliente).
/// </summary>
[ApiController]
[Route("api/contracts")]
[Authorize]
[RequireModule(ModuleCodes.Alquileres)]
public class ContractsController : ControllerBase
{
    private readonly IContractService _service;

    public ContractsController(IContractService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = PermissionCodes.ContractsView)]
    public async Task<ActionResult<IReadOnlyList<ContractResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _service.GetAllAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionCodes.ContractsView)]
    public async Task<ActionResult<ContractResponse>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.GetByIdAsync(id, cancellationToken));

    [HttpGet("by-rental/{rentalId:guid}")]
    [Authorize(Policy = PermissionCodes.ContractsView)]
    public async Task<ActionResult<ContractResponse>> GetByRental(Guid rentalId, CancellationToken cancellationToken)
    {
        var contract = await _service.GetByRentalIdAsync(rentalId, cancellationToken);
        return contract is null ? NotFound() : Ok(contract);
    }

    /// <summary>PDF del contrato. Sin [Authorize] de policy propia adicional: alcanza con estar logueado y que el contrato pertenezca al negocio (se valida en el servicio); se embebe directo en un &lt;iframe&gt; desde el frontend.</summary>
    [HttpGet("{id:guid}/pdf")]
    [Authorize(Policy = PermissionCodes.ContractsView)]
    public async Task<IActionResult> GetPdf(Guid id, CancellationToken cancellationToken)
    {
        var (bytes, fileName) = await _service.GetPdfAsync(id, cancellationToken);
        return File(bytes, "application/pdf", fileName);
    }

    /// <summary>Registra la firma de un contrato pendiente (ej: rentas creadas manualmente en el panel) y reenvía el PDF firmado por correo.</summary>
    [HttpPost("{id:guid}/sign")]
    [Authorize(Policy = PermissionCodes.ContractsManage)]
    public async Task<ActionResult<ContractResponse>> Sign(Guid id, SignContractRequest request, CancellationToken cancellationToken)
        => Ok(await _service.SignAsync(id, request, cancellationToken));

    /// <summary>Reenvía por correo el PDF ya generado de un contrato, a demanda.</summary>
    [HttpPost("{id:guid}/resend-email")]
    [Authorize(Policy = PermissionCodes.ContractsManage)]
    public async Task<ActionResult<ContractResponse>> ResendEmail(Guid id, CancellationToken cancellationToken)
        => Ok(await _service.ResendEmailAsync(id, cancellationToken));
}
