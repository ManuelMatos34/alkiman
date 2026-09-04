using Alkiman.API.Authorization;
using Alkiman.Application.Common.Modules;
using Alkiman.Application.Common.Permissions;
using Alkiman.Application.RentalImports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alkiman.API.Controllers;

/// <summary>Mantenimiento de migración: importa en lote alquileres ya existentes (otro sistema o manual).</summary>
[ApiController]
[Route("api/rentals/import")]
[Authorize]
[RequireModule(ModuleCodes.Alquileres)]
public class RentalImportController : ControllerBase
{
    private readonly IRentalImportService _service;

    public RentalImportController(IRentalImportService service)
    {
        _service = service;
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.RentalsManage)]
    public async Task<ActionResult<ImportRentalsResponse>> Import(ImportRentalsRequest request, CancellationToken cancellationToken)
        => Ok(await _service.ImportAsync(request, cancellationToken));
}
