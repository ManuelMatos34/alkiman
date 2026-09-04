using Alkiman.API.Authorization;
using Alkiman.Application.Common.Modules;
using Alkiman.Application.Common.Permissions;
using Alkiman.Application.Emails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alkiman.API.Controllers;

[ApiController]
[Route("api/emails")]
[Authorize]
[RequireModule(ModuleCodes.Alquileres)]
public class EmailsController : ControllerBase
{
    private readonly IEmailService _service;

    public EmailsController(IEmailService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = PermissionCodes.EmailsView)]
    public async Task<ActionResult<IReadOnlyList<EmailMessageResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _service.GetAllAsync(cancellationToken));

    [HttpPost("individual")]
    [Authorize(Policy = PermissionCodes.EmailsManage)]
    public async Task<ActionResult<EmailMessageResponse>> SendIndividual(SendIndividualEmailRequest request, CancellationToken cancellationToken)
        => Ok(await _service.SendIndividualAsync(request, cancellationToken));

    [HttpPost("mass")]
    [Authorize(Policy = PermissionCodes.EmailsManage)]
    public async Task<ActionResult<SendMassEmailResponse>> SendMass(SendMassEmailRequest request, CancellationToken cancellationToken)
        => Ok(await _service.SendMassAsync(request, cancellationToken));
}
