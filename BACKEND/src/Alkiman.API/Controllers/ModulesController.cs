using Alkiman.Application.Modules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alkiman.API.Controllers;

[ApiController]
[Route("api/modules")]
[Authorize]
public class ModulesController : ControllerBase
{
    private readonly IModuleService _service;

    public ModulesController(IModuleService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ModuleResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _service.GetAllForCurrentLandlordAsync(cancellationToken));

    /// <summary>
    /// Habilita un módulo gratuito (del catálogo, con IsAvailable=1) para el negocio
    /// autenticado. Sin policy de permiso específico: cualquier usuario autenticado del
    /// negocio puede activar un módulo gratuito, igual que hoy puede ver el selector.
    /// </summary>
    [HttpPost("{code}/enable")]
    public async Task<IActionResult> Enable(string code, CancellationToken cancellationToken)
    {
        await _service.EnableModuleAsync(code, cancellationToken);
        return NoContent();
    }
}
