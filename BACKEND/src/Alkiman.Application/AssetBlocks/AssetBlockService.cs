using Alkiman.Application.Assets;
using Alkiman.Application.AuditLogs;
using Alkiman.Application.Common.Exceptions;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Domain.Entities;
using Alkiman.Domain.Enums;

namespace Alkiman.Application.AssetBlocks;

public class AssetBlockService : IAssetBlockService
{
    private readonly IAssetBlockRepository _repository;
    private readonly IAssetRepository _assetRepository;
    private readonly ICurrentLandlordService _currentLandlord;
    private readonly IAuditLogService _auditLog;

    public AssetBlockService(
        IAssetBlockRepository repository,
        IAssetRepository assetRepository,
        ICurrentLandlordService currentLandlord,
        IAuditLogService auditLog)
    {
        _repository = repository;
        _assetRepository = assetRepository;
        _currentLandlord = currentLandlord;
        _auditLog = auditLog;
    }

    public async Task<IReadOnlyList<AssetBlockResponse>> GetAllByAssetAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        await EnsureAssetOwnedAsync(assetId, cancellationToken);
        var blocks = await _repository.GetAllByAssetAsync(assetId, cancellationToken);
        return blocks.Select(ToResponse).ToList();
    }

    public async Task<AssetBlockResponse> CreateAsync(CreateAssetBlockRequest request, CancellationToken cancellationToken = default)
    {
        if (request.EndDate <= request.StartDate)
            throw new AppValidationException("La fecha de fin debe ser posterior a la fecha de inicio.");

        await EnsureAssetOwnedAsync(request.AssetId, cancellationToken);

        var block = new AssetBlock
        {
            Id = Guid.NewGuid(),
            AssetId = request.AssetId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Reason = request.Reason,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentLandlord.UserId
        };

        await _repository.CreateAsync(block, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Create, "INV_AssetBlocks", block.Id.ToString(), null, block, cancellationToken);
        return ToResponse(block);
    }

    public async Task<AssetBlockResponse> UpdateAsync(Guid id, UpdateAssetBlockRequest request, CancellationToken cancellationToken = default)
    {
        if (request.EndDate <= request.StartDate)
            throw new AppValidationException("La fecha de fin debe ser posterior a la fecha de inicio.");

        var block = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(AssetBlock), id);
        await EnsureAssetOwnedAsync(block.AssetId, cancellationToken);

        block.StartDate = request.StartDate;
        block.EndDate = request.EndDate;
        block.Reason = request.Reason;
        block.UpdatedAt = DateTime.UtcNow;
        block.UpdatedBy = _currentLandlord.UserId;

        await _repository.UpdateAsync(block, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Update, "INV_AssetBlocks", block.Id.ToString(), null, block, cancellationToken);
        return ToResponse(block);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var block = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(AssetBlock), id);
        await EnsureAssetOwnedAsync(block.AssetId, cancellationToken);

        await _repository.DeleteAsync(id, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Delete, "INV_AssetBlocks", block.Id.ToString(), block, null, cancellationToken);
    }

    private async Task EnsureAssetOwnedAsync(Guid assetId, CancellationToken cancellationToken)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var asset = await _assetRepository.GetByIdAsync(assetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), assetId);

        if (asset.LandlordId != landlordId)
            throw new ForbiddenException("El activo no pertenece al negocio autenticado.");
    }

    private static AssetBlockResponse ToResponse(AssetBlock block) =>
        new(block.Id, block.AssetId, block.StartDate, block.EndDate, block.Reason);
}
