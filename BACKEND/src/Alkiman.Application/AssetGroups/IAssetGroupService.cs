namespace Alkiman.Application.AssetGroups;

public interface IAssetGroupService
{
    Task<IReadOnlyList<AssetGroupResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AssetGroupResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AssetGroupResponse> CreateAsync(CreateAssetGroupRequest request, CancellationToken cancellationToken = default);
    Task<AssetGroupResponse> UpdateAsync(int id, UpdateAssetGroupRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
