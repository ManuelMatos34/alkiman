using Alkiman.Domain.Entities;

namespace Alkiman.Application.Carwash;

public interface ICarwashWasherRepository
{
    Task<IReadOnlyList<CarwashWasher>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default);
    Task<CarwashWasher?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// El único lavador activo del negocio, o null si hay ninguno o más de uno.
    ///
    /// Es lo que sostiene el auto-asignado del modo Solitario: ahí hay una sola
    /// persona lavando, así que "el único activo" la identifica sin ambigüedad y
    /// sin necesidad de vincular la ficha con una cuenta del sistema. Si aparece
    /// más de uno devuelve null a propósito y la asignación vuelve a ser manual.
    /// </summary>
    Task<CarwashWasher?> GetSingleActiveAsync(Guid landlordId, CancellationToken cancellationToken = default);

    Task CreateAsync(CarwashWasher washer, CancellationToken cancellationToken = default);
    Task UpdateAsync(CarwashWasher washer, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Si el lavador ya tiene tickets, no se puede eliminar (violaría la FK de CWS_Tickets) — hay que desactivarlo, así el historial conserva el nombre.</summary>
    Task<bool> HasTicketsAsync(Guid washerId, CancellationToken cancellationToken = default);
}
