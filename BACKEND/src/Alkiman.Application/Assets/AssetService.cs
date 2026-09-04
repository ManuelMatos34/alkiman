using Alkiman.Application.AuditLogs;
using Alkiman.Application.Common.Exceptions;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Domain.Entities;
using Alkiman.Domain.Enums;

namespace Alkiman.Application.Assets;

public class AssetService : IAssetService
{
    private readonly IAssetRepository _repository;
    private readonly ICurrentLandlordService _currentLandlord;
    private readonly IAuditLogService _auditLog;

    public AssetService(IAssetRepository repository, ICurrentLandlordService currentLandlord, IAuditLogService auditLog)
    {
        _repository = repository;
        _currentLandlord = currentLandlord;
        _auditLog = auditLog;
    }

    public async Task<IReadOnlyList<AssetResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var assets = await _repository.GetAllByLandlordAsync(landlordId, cancellationToken);
        return assets.Select(ToResponse).ToList();
    }

    public async Task<AssetResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var asset = await GetOwnedOrThrowAsync(id, cancellationToken);
        return ToResponse(asset);
    }

    public async Task<AssetResponse> CreateAsync(CreateAssetRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Stock < 0)
            throw new AppValidationException("El stock no puede ser negativo.");
        if (request.BasePrice < 0)
            throw new AppValidationException("El precio base no puede ser negativo.");

        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            LandlordId = landlordId,
            CategoryId = request.CategoryId,
            Name = request.Name,
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            Status = AssetStatus.Available,
            RentalType = request.RentalType,
            BasePrice = request.BasePrice,
            Stock = request.Stock,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentLandlord.UserId
        };

        await _repository.CreateAsync(asset, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Create, "INV_Assets", asset.Id.ToString(), null, asset, cancellationToken);
        return ToResponse(asset);
    }

    public async Task<AssetResponse> UpdateAsync(Guid id, UpdateAssetRequest request, CancellationToken cancellationToken = default)
    {
        var asset = await GetOwnedOrThrowAsync(id, cancellationToken);

        asset.CategoryId = request.CategoryId;
        asset.Name = request.Name;
        asset.Description = request.Description;
        asset.ImageUrl = request.ImageUrl;
        asset.BasePrice = request.BasePrice;
        asset.Stock = request.Stock;
        asset.UpdatedAt = DateTime.UtcNow;
        asset.UpdatedBy = _currentLandlord.UserId;

        await _repository.UpdateAsync(asset, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Update, "INV_Assets", asset.Id.ToString(), null, asset, cancellationToken);
        return ToResponse(asset);
    }

    public async Task<AssetResponse> UpdateStatusAsync(Guid id, UpdateAssetStatusRequest request, CancellationToken cancellationToken = default)
    {
        var asset = await GetOwnedOrThrowAsync(id, cancellationToken);

        asset.Status = request.Status;
        asset.UpdatedAt = DateTime.UtcNow;
        asset.UpdatedBy = _currentLandlord.UserId;

        await _repository.UpdateAsync(asset, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Update, "INV_Assets", asset.Id.ToString(), null, asset, cancellationToken);
        return ToResponse(asset);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var asset = await GetOwnedOrThrowAsync(id, cancellationToken);
        await _repository.DeleteAsync(id, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Delete, "INV_Assets", asset.Id.ToString(), asset, null, cancellationToken);
    }

    private async Task<Asset> GetOwnedOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var asset = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), id);

        if (asset.LandlordId != landlordId)
            throw new ForbiddenException("El activo no pertenece al negocio autenticado.");

        return asset;
    }

    private static AssetResponse ToResponse(Asset asset) => new(
        asset.Id, asset.CategoryId, asset.Name, asset.Description, asset.ImageUrl,
        asset.Status, asset.RentalType, asset.BasePrice, asset.Stock, asset.CreatedAt);
}
