using Alkiman.API.Authorization;
using Alkiman.Application.Common.Modules;
using Alkiman.Application.Common.Permissions;
using Alkiman.Application.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alkiman.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
[RequireModule(ModuleCodes.Alquileres)]
public class ReportsController : ControllerBase
{
    private readonly IReportService _service;

    public ReportsController(IReportService service)
    {
        _service = service;
    }

    /// <summary>
    /// Tablero de métricas del negocio autenticado.
    /// Los datos de inventario y rentas activas son siempre el estado actual (sin filtro de fechas).
    /// Los datos financieros (ingresos, egresos, rankings) se filtran por fecha de pago cuando se especifica el rango.
    /// </summary>
    /// <param name="from">Fecha de inicio inclusiva (yyyy-MM-dd) para datos financieros. Opcional.</param>
    /// <param name="to">Fecha de fin inclusiva (yyyy-MM-dd) para datos financieros. Opcional.</param>
    [HttpGet("summary")]
    [Authorize(Policy = PermissionCodes.ReportsView)]
    public async Task<ActionResult<ReportSummaryResponse>> GetSummary(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
        => Ok(await _service.GetSummaryAsync(from, to, cancellationToken));
}
