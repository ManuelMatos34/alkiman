using Alkiman.Application.Common.Exceptions;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Customers;
using Alkiman.Domain.Entities;

namespace Alkiman.Application.Reminders;

public class ReminderService : IReminderService
{
    private static readonly string[] ValidStatuses = { "Pending", "Completed", "Cancelled" };

    private readonly IReminderRepository _repository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICurrentLandlordService _currentLandlord;

    public ReminderService(
        IReminderRepository repository,
        ICustomerRepository customerRepository,
        ICurrentLandlordService currentLandlord)
    {
        _repository = repository;
        _customerRepository = customerRepository;
        _currentLandlord = currentLandlord;
    }

    public async Task<IReadOnlyList<ReminderResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var reminders = await _repository.GetAllByLandlordAsync(landlordId, cancellationToken);
        return reminders.Select(ToResponse).ToList();
    }

    public async Task<ReminderResponse> CreateAsync(CreateReminderRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new AppValidationException("El título del recordatorio es obligatorio.");

        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);

        if (request.CustomerId.HasValue)
            await EnsureCustomerOwnedAsync(request.CustomerId.Value, landlordId, cancellationToken);

        var reminder = new Reminder
        {
            LandlordId = landlordId,
            CustomerId = request.CustomerId,
            RentalId = request.RentalId,
            Title = request.Title,
            Message = request.Message,
            RemindAt = request.RemindAt,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentLandlord.UserId
        };

        reminder.Id = await _repository.CreateAsync(reminder, cancellationToken);
        return await ToResponseWithCustomerAsync(reminder, cancellationToken);
    }

    public async Task<ReminderResponse> UpdateAsync(Guid id, UpdateReminderRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new AppValidationException("El título del recordatorio es obligatorio.");
        if (!ValidStatuses.Contains(request.Status))
            throw new AppValidationException("El estado del recordatorio no es válido.");

        var reminder = await GetOwnedOrThrowAsync(id, cancellationToken);
        var landlordId = reminder.LandlordId;

        if (request.CustomerId.HasValue)
            await EnsureCustomerOwnedAsync(request.CustomerId.Value, landlordId, cancellationToken);

        reminder.Title = request.Title;
        reminder.Message = request.Message;
        reminder.RemindAt = request.RemindAt;
        reminder.CustomerId = request.CustomerId;
        reminder.RentalId = request.RentalId;
        reminder.Status = request.Status;
        reminder.UpdatedAt = DateTime.UtcNow;
        reminder.UpdatedBy = _currentLandlord.UserId;

        await _repository.UpdateAsync(reminder, cancellationToken);
        return await ToResponseWithCustomerAsync(reminder, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await GetOwnedOrThrowAsync(id, cancellationToken);
        await _repository.DeleteAsync(id, cancellationToken);
    }

    private async Task EnsureCustomerOwnedAsync(Guid customerId, Guid landlordId, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), customerId);
        if (customer.LandlordId != landlordId)
            throw new ForbiddenException("El cliente no pertenece al negocio autenticado.");
    }

    private async Task<Reminder> GetOwnedOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var reminder = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Reminder), id);

        if (reminder.LandlordId != landlordId)
            throw new ForbiddenException("El recordatorio no pertenece al negocio autenticado.");

        return reminder;
    }

    private async Task<ReminderResponse> ToResponseWithCustomerAsync(Reminder reminder, CancellationToken cancellationToken)
    {
        string? customerName = null;
        if (reminder.CustomerId.HasValue)
        {
            var customer = await _customerRepository.GetByIdAsync(reminder.CustomerId.Value, cancellationToken);
            customerName = customer?.FullName;
        }

        return new ReminderResponse(
            reminder.Id, reminder.Title, reminder.Message, reminder.RemindAt,
            reminder.CustomerId, customerName, reminder.RentalId, reminder.Status, reminder.CreatedAt);
    }

    private static ReminderResponse ToResponse(ReminderRaw raw) => new(
        raw.Id, raw.Title, raw.Message, raw.RemindAt, raw.CustomerId, raw.CustomerName,
        raw.RentalId, raw.Status, raw.CreatedAt);
}
