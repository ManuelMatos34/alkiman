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
}
