using Alkiman.Domain.Entities;

namespace Alkiman.Application.Assets;

public interface IAssetRepository
{
    Task<IReadOnlyList<Asset>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default);
    Task<Asset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(Asset asset, CancellationToken cancellationToken = default);
    Task UpdateAsync(Asset asset, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
