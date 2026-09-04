namespace Alkiman.Application.PortalLinks;

public record PortalLinkResponse(
    Guid Id,
    int AssetGroupId,
    string AssetGroupName,
    string Title,
    string Slug,
    bool IsActive,
    DateTime CreatedAt);

public record CreatePortalLinkRequest(string Title, int AssetGroupId);

public record UpdatePortalLinkRequest(string Title, int AssetGroupId, bool IsActive);
