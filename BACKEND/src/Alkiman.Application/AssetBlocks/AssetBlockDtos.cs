namespace Alkiman.Application.AssetBlocks;

public record AssetBlockResponse(Guid Id, Guid AssetId, DateTime StartDate, DateTime EndDate, string? Reason);

public record CreateAssetBlockRequest(Guid AssetId, DateTime StartDate, DateTime EndDate, string? Reason);

public record UpdateAssetBlockRequest(DateTime StartDate, DateTime EndDate, string? Reason);
