namespace Alkiman.Application.AssetGroups;

public record AssetGroupResponse(
    int Id,
    string Name,
    string? Description,
    IReadOnlyList<Guid> AssetIds,
    int AssetsCount,
    DateTime CreatedAt);

public record CreateAssetGroupRequest(string Name, string? Description, IReadOnlyList<Guid> AssetIds);

public record UpdateAssetGroupRequest(string Name, string? Description, IReadOnlyList<Guid> AssetIds);
