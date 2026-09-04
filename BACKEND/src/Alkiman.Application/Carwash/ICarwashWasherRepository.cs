using Alkiman.Domain.Entities;

namespace Alkiman.Application.Carwash;

public interface ICarwashWasherRepository
{
    Task<IReadOnlyList<CarwashWasher>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default);
    Task<CarwashWasher?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>El lavador vinculado a una cuenta del sistema, si esa cuenta tiene uno. Ver <see cref="CarwashWasher.UserId"/>.</summary>
    Task<CarwashWasher?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task CreateAsync(CarwashWasher washer, CancellationToken cancellationToken = default);
    Task UpdateAsync(CarwashWasher washer, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Si el lavador ya tiene tickets, no se puede eliminar (violaría la FK de CWS_Tickets) — hay que desactivarlo, así el historial conserva el nombre.</summary>
    Task<bool> HasTicketsAsync(Guid washerId, CancellationToken cancellationToken = default);
}
