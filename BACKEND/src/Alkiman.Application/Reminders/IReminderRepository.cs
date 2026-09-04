using Alkiman.Domain.Entities;

namespace Alkiman.Application.Reminders;

public interface IReminderRepository
{
    Task<IReadOnlyList<ReminderRaw>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default);
    Task<Reminder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(Reminder reminder, CancellationToken cancellationToken = default);
    Task UpdateAsync(Reminder reminder, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Indica si ya existe un recordatorio automático (creado por el job de vencimientos) para esta renta, para no duplicarlo en corridas posteriores.</summary>
    Task<bool> ExistsAutoReminderForRentalAsync(Guid rentalId, CancellationToken cancellationToken = default);
}

/// <summary>Constantes compartidas por los recordatorios generados automáticamente (no manuales) por jobs de background.</summary>
public static class AutoReminderDefaults
{
    /// <summary>Valor de CreatedBy usado para marcar un Reminder como generado automáticamente por el job de vencimiento de rentas, y así poder distinguirlo de recordatorios manuales al chequear duplicados.</summary>
    public const string RentalDueCreatedBy = "system:rental-due-reminder";
}
