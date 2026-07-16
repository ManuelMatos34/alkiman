using Alkiman.Domain.Entities;

namespace Alkiman.Application.Rentals;

public interface IRentalRepository
{
    /// <summary>Rentas de todos los activos del landlord (para el tablero de calendario).</summary>
    Task<IReadOnlyList<Rental>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Rental>> GetAllByAssetAsync(Guid assetId, CancellationToken cancellationToken = default);
    Task<Rental?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(Rental rental, CancellationToken cancellationToken = default);
    Task UpdateAsync(Rental rental, CancellationToken cancellationToken = default);
}
