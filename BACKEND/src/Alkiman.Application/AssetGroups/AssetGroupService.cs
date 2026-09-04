using Alkiman.Application.AuditLogs;
using Alkiman.Application.Common.Exceptions;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Domain.Entities;
using Alkiman.Domain.Enums;

namespace Alkiman.Application.AssetGroups;

public class AssetGroupService : IAssetGroupService
{
    private readonly IAssetGroupRepository _repository;
    private readonly ICurrentLandlordService _currentLandlord;
    private readonly IAuditLogService _auditLog;

    public AssetGroupService(IAssetGroupRepository repository, ICurrentLandlordService currentLandlord, IAuditLogService auditLog)
    {
        _repository = repository;
        _currentLandlord = currentLandlord;
        _auditLog = auditLog;
    }

    public async Task<IReadOnlyList<AssetGroupResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var groups = await _repository.GetAllByLandlordAsync(landlordId, cancellationToken);

        var responses = new List<AssetGroupResponse>(groups.Count);
        foreach (var group in groups)
        {
            responses.Add(await ToResponseAsync(group, cancellationToken));
        }
        return responses;
    }

    public async Task<AssetGroupResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var group = await GetOwnedOrThrowAsync(id, cancellationToken);
        return await ToResponseAsync(group, cancellationToken);
    }

    public async Task<AssetGroupResponse> CreateAsync(CreateAssetGroupRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 150)
            throw new AppValidationException("El nombre del grupo es inválido.");

        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var name = request.Name.Trim();

        if (await _repository.NameExistsAsync(landlordId, name, null, cancellationToken))
            throw new AppValidationException("Ya existe un grupo con ese nombre.");

        var assetIds = await ResolveAssetIdsAsync(landlordId, request.AssetIds, cancellationToken);

        var group = new AssetGroup
        {
            LandlordId = landlordId,
            Name = name,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentLandlord.UserId,
        };

        group.Id = await _repository.CreateAsync(group, assetIds, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Create, "INV_AssetGroups", group.Id.ToString(), null, group, cancellationToken);
        return await ToResponseAsync(group, cancellationToken);
    }

    public async Task<AssetGroupResponse> UpdateAsync(int id, UpdateAssetGroupRequest request, CancellationToken cancellationToken = default)
    {
        var group = await GetOwnedOrThrowAsync(id, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 150)
            throw new AppValidationException("El nombre del grupo es inválido.");

        var name = request.Name.Trim();

        if (await _repository.NameExistsAsync(group.LandlordId, name, id, cancellationToken))
            throw new AppValidationException("Ya existe un grupo con ese nombre.");

        var assetIds = await ResolveAssetIdsAsync(group.LandlordId, request.AssetIds, cancellationToken);

        group.Name = name;
        group.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        group.UpdatedAt = DateTime.UtcNow;
        group.UpdatedBy = _currentLandlord.UserId;

        await _repository.UpdateAsync(group, assetIds, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Update, "INV_AssetGroups", group.Id.ToString(), null, group, cancellationToken);
        return await ToResponseAsync(group, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var group = await GetOwnedOrThrowAsync(id, cancellationToken);
        await _repository.DeleteAsync(id, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Delete, "INV_AssetGroups", group.Id.ToString(), group, null, cancellationToken);
    }

    private async Task<List<Guid>> ResolveAssetIdsAsync(Guid landlordId, IReadOnlyList<Guid> assetIds, CancellationToken cancellationToken)
    {
        var distinct = assetIds.Distinct().ToList();
        if (distinct.Count == 0)
            return distinct;

        var owned = await _repository.FilterOwnedAssetIdsAsync(landlordId, distinct, cancellationToken);
        if (owned.Count != distinct.Count)
            throw new AppValidationException("Uno o más activos seleccionados no son válidos.");

        return distinct;
    }

    private async Task<AssetGroup> GetOwnedOrThrowAsync(int id, CancellationToken cancellationToken)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var group = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(AssetGroup), id);

        if (group.LandlordId != landlordId)
            throw new ForbiddenException("El grupo no pertenece al negocio autenticado.");

        return group;
    }

    private async Task<AssetGroupResponse> ToResponseAsync(AssetGroup group, CancellationToken cancellationToken)
    {
        var assetIds = await _repository.GetMemberAssetIdsAsync(group.Id, cancellationToken);
        return new AssetGroupResponse(group.Id, group.Name, group.Description, assetIds, assetIds.Count, group.CreatedAt);
    }
}
