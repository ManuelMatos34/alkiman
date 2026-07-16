using Alkiman.Domain.Enums;

namespace Alkiman.Application.Assets;

public record AssetResponse(
    Guid Id,
    int CategoryId,
    string Name,
    string? Description,
    string? ImageUrl,
    AssetStatus Status,
    RentalTypeOption RentalType,
    decimal BasePrice,
    int Stock,
    DateTime CreatedAt);

public record CreateAssetRequest(
    int CategoryId,
    string Name,
    string? Description,
    string? ImageUrl,
    RentalTypeOption RentalType,
    decimal BasePrice,
    int Stock);

public record UpdateAssetRequest(
    int CategoryId,
    string Name,
    string? Description,
    string? ImageUrl,
    decimal BasePrice,
    int Stock);

public record UpdateAssetStatusRequest(AssetStatus Status);
