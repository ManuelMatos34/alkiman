using Alkiman.API.Authorization;
using Alkiman.Application.Common.Modules;
using Alkiman.Application.Common.Permissions;
using Alkiman.Application.Reminders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alkiman.API.Controllers;

[ApiController]
[Route("api/reminders")]
[Authorize]
[RequireModule(ModuleCodes.Alquileres)]
public class RemindersController : ControllerBase
{
    private readonly IReminderService _service;

    public RemindersController(IReminderService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = PermissionCodes.EmailsView)]
    public async Task<ActionResult<IReadOnlyList<ReminderResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _service.GetAllAsync(cancellationToken));

    [HttpPost]
    [Authorize(Policy = PermissionCodes.EmailsManage)]
    public async Task<ActionResult<ReminderResponse>> Create(CreateReminderRequest request, CancellationToken cancellationToken)
        => Ok(await _service.CreateAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionCodes.EmailsManage)]
    public async Task<ActionResult<ReminderResponse>> Update(Guid id, UpdateReminderRequest request, CancellationToken cancellationToken)
        => Ok(await _service.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PermissionCodes.EmailsManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
