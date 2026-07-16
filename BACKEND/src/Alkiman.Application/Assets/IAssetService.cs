namespace Alkiman.Application.Assets;

public interface IAssetService
{
    Task<IReadOnlyList<AssetResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AssetResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AssetResponse> CreateAsync(CreateAssetRequest request, CancellationToken cancellationToken = default);
    Task<AssetResponse> UpdateAsync(Guid id, UpdateAssetRequest request, CancellationToken cancellationToken = default);
    Task<AssetResponse> UpdateStatusAsync(Guid id, UpdateAssetStatusRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
