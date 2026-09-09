using Alkiman.Application.WhatsApp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alkiman.API.Controllers;

[ApiController]
[Route("api/whatsapp")]
[Authorize]
public class WhatsAppController : ControllerBase
{
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromServices] IWhatsAppStatsService svc, CancellationToken ct)
        => Ok(await svc.GetStatsAsync(ct));

    [HttpGet("config")]
    public async Task<IActionResult> GetConfig([FromServices] IWhatsAppConfigService svc, CancellationToken ct)
        => Ok(await svc.GetConfigAsync(ct));

    [HttpPut("config")]
    public async Task<IActionResult> SaveConfig([FromBody] WhatsAppConfigRequest request, [FromServices] IWhatsAppConfigService svc, CancellationToken ct)
        => Ok(await svc.SaveConfigAsync(request, ct));
}
