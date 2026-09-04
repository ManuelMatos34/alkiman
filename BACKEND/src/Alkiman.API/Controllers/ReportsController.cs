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

    /// <summary>Tablero de métricas del negocio autenticado: finanzas, rentas, activos y rankings.</summary>
    [HttpGet("summary")]
    [Authorize(Policy = PermissionCodes.ReportsView)]
    public async Task<ActionResult<ReportSummaryResponse>> GetSummary(CancellationToken cancellationToken)
        => Ok(await _service.GetSummaryAsync(cancellationToken));
}
