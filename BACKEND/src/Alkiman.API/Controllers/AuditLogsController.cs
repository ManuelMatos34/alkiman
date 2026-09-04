using Alkiman.Application.AuditLogs;
using Alkiman.Application.Common.Permissions;
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
    [Authorize(Policy = PermissionCodes.AuditView)]
    public async Task<ActionResult<IReadOnlyList<AuditLogResponse>>> GetMyActivity(CancellationToken cancellationToken)
        => Ok(await _service.GetMyActivityAsync(cancellationToken));
}
