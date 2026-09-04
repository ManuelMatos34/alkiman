using Alkiman.API.Authorization;
using Alkiman.Application.Common.Modules;
using Alkiman.Application.Common.Permissions;
using Alkiman.Application.ContractTemplates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alkiman.API.Controllers;

/// <summary>
/// Mantenimiento de plantillas de contrato: un negocio puede tener varias plantillas por
/// Categoría (historial de versiones) y elige cuál es la activa por categoría (la que se
/// usa para generar el contrato cuando se crea una renta de un activo de esa categoría).
/// </summary>
[ApiController]
[Route("api/contract-templates")]
[Authorize]
[RequireModule(ModuleCodes.Alquileres)]
public class ContractTemplatesController : ControllerBase
{
    private readonly IContractTemplateService _service;

    public ContractTemplatesController(IContractTemplateService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = PermissionCodes.ContractsView)]
    public async Task<ActionResult<IReadOnlyList<ContractTemplateResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _service.GetAllAsync(cancellationToken));

    [HttpGet("{id:int}")]
    [Authorize(Policy = PermissionCodes.ContractsView)]
    public async Task<ActionResult<ContractTemplateResponse>> GetById(int id, CancellationToken cancellationToken)
        => Ok(await _service.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = PermissionCodes.ContractsManage)]
    public async Task<ActionResult<ContractTemplateResponse>> Create(CreateContractTemplateRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = PermissionCodes.ContractsManage)]
    public async Task<ActionResult<ContractTemplateResponse>> Update(int id, UpdateContractTemplateRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:int}")]
    [Authorize(Policy = PermissionCodes.ContractsManage)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Marca esta plantilla como la activa de su categoría (desactiva cualquier otra activa de esa categoría).</summary>
    [HttpPost("{id:int}/activate")]
    [Authorize(Policy = PermissionCodes.ContractsManage)]
    public async Task<ActionResult<ContractTemplateResponse>> Activate(int id, CancellationToken cancellationToken)
        => Ok(await _service.ActivateAsync(id, cancellationToken));
}
