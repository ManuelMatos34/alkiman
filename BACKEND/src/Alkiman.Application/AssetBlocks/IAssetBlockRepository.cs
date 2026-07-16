using Alkiman.Domain.Entities;

namespace Alkiman.Application.AssetBlocks;

public interface IAssetBlockRepository
{
    Task<IReadOnlyList<AssetBlock>> GetAllByAssetAsync(Guid assetId, CancellationToken cancellationToken = default);
    Task<AssetBlock?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(AssetBlock block, CancellationToken cancellationToken = default);
    Task UpdateAsync(AssetBlock block, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
