namespace Alkiman.Application.Reminders;

public interface IReminderService
{
    Task<IReadOnlyList<ReminderResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ReminderResponse> CreateAsync(CreateReminderRequest request, CancellationToken cancellationToken = default);
    Task<ReminderResponse> UpdateAsync(Guid id, UpdateReminderRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
