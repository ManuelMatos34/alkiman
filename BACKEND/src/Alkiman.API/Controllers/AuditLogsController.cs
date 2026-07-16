using Alkiman.Application.AuditLogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alkiman.API.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _service;

    public AuditLogsController(IAuditLogService service)
    {
        _service = service;
    }

    /// <summary>Bitácora de acciones del usuario autenticado.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<IReadOnlyList<AuditLogResponse>>> GetMyActivity(CancellationToken cancellationToken)
        => Ok(await _service.GetMyActivityAsync(cancellationToken));
}
