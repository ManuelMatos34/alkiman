namespace Alkiman.Application.Reminders;

public record ReminderResponse(
    Guid Id,
    string Title,
    string? Message,
    DateTime RemindAt,
    Guid? CustomerId,
    string? CustomerName,
    Guid? RentalId,
    string Status,
    DateTime CreatedAt
);

public record CreateReminderRequest(
    string Title,
    string? Message,
    DateTime RemindAt,
    Guid? CustomerId,
    Guid? RentalId
);

public record UpdateReminderRequest(
    string Title,
    string? Message,
    DateTime RemindAt,
    Guid? CustomerId,
    Guid? RentalId,
    string Status
);

/// <summary>Fila cruda devuelta por el repositorio, con el nombre del cliente ya resuelto por JOIN.</summary>
public record ReminderRaw(
    Guid Id,
    string Title,
    string? Message,
    DateTime RemindAt,
    Guid? CustomerId,
    string? CustomerName,
    Guid? RentalId,
    string Status,
    DateTime CreatedAt
);
