using Alkiman.Domain.Entities;

namespace Alkiman.Application.AssetGroups;

public interface IAssetGroupRepository
{
    Task<IReadOnlyList<AssetGroup>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default);
    Task<AssetGroup?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(Guid landlordId, string name, int? excludeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetMemberAssetIdsAsync(int groupId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> FilterOwnedAssetIdsAsync(Guid landlordId, IEnumerable<Guid> assetIds, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(AssetGroup group, IEnumerable<Guid> assetIds, CancellationToken cancellationToken = default);
    Task UpdateAsync(AssetGroup group, IEnumerable<Guid> assetIds, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
