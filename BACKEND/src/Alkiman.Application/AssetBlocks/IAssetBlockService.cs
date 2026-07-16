namespace Alkiman.Application.AssetBlocks;

public interface IAssetBlockService
{
    Task<IReadOnlyList<AssetBlockResponse>> GetAllByAssetAsync(Guid assetId, CancellationToken cancellationToken = default);
    Task<AssetBlockResponse> CreateAsync(CreateAssetBlockRequest request, CancellationToken cancellationToken = default);
    Task<AssetBlockResponse> UpdateAsync(Guid id, UpdateAssetBlockRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
