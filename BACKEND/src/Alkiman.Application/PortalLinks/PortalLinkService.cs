using System.Text;
using Alkiman.Application.AssetGroups;
using Alkiman.Application.AuditLogs;
using Alkiman.Application.Common.Exceptions;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Domain.Entities;
using Alkiman.Domain.Enums;

namespace Alkiman.Application.PortalLinks;

/// <summary>
/// Administración de links públicos del Portal de Rentas. Cada link se genera a partir
/// de un Grupo de Activos ya existente: quien reciba el link ve solo los activos de ese
/// grupo (ver <see cref="Portal.IPortalService"/> para el catálogo/checkout público).
/// </summary>
public class PortalLinkService : IPortalLinkService
{
    private readonly IPortalLinkRepository _repository;
    private readonly IAssetGroupRepository _assetGroupRepository;
    private readonly ICurrentLandlordService _currentLandlord;
    private readonly IAuditLogService _auditLog;

    public PortalLinkService(
        IPortalLinkRepository repository,
        IAssetGroupRepository assetGroupRepository,
        ICurrentLandlordService currentLandlord,
        IAuditLogService auditLog)
    {
        _repository = repository;
        _assetGroupRepository = assetGroupRepository;
        _currentLandlord = currentLandlord;
        _auditLog = auditLog;
    }

    public async Task<IReadOnlyList<PortalLinkResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var links = await _repository.GetAllByLandlordAsync(landlordId, cancellationToken);

        var responses = new List<PortalLinkResponse>(links.Count);
        foreach (var link in links)
        {
            responses.Add(await ToResponseAsync(link, cancellationToken));
        }
        return responses;
    }

    public async Task<PortalLinkResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var link = await GetOwnedOrThrowAsync(id, cancellationToken);
        return await ToResponseAsync(link, cancellationToken);
    }

    public async Task<PortalLinkResponse> CreateAsync(CreatePortalLinkRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 150)
            throw new AppValidationException("El título del link es inválido.");

        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var assetGroup = await GetOwnedAssetGroupOrThrowAsync(landlordId, request.AssetGroupId, cancellationToken);

        var title = request.Title.Trim();
        var link = new PortalLink
        {
            Id = Guid.NewGuid(),
            LandlordId = landlordId,
            AssetGroupId = assetGroup.Id,
            Title = title,
            Slug = await GenerateUniqueSlugAsync(title, cancellationToken),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentLandlord.UserId
        };

        await _repository.CreateAsync(link, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Create, "PRT_PortalLinks", link.Id.ToString(), null, link, cancellationToken);
        return await ToResponseAsync(link, cancellationToken);
    }

    public async Task<PortalLinkResponse> UpdateAsync(Guid id, UpdatePortalLinkRequest request, CancellationToken cancellationToken = default)
    {
        var link = await GetOwnedOrThrowAsync(id, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 150)
            throw new AppValidationException("El título del link es inválido.");

        var assetGroup = await GetOwnedAssetGroupOrThrowAsync(link.LandlordId, request.AssetGroupId, cancellationToken);

        link.Title = request.Title.Trim();
        link.AssetGroupId = assetGroup.Id;
        link.IsActive = request.IsActive;
        link.UpdatedAt = DateTime.UtcNow;
        link.UpdatedBy = _currentLandlord.UserId;

        await _repository.UpdateAsync(link, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Update, "PRT_PortalLinks", link.Id.ToString(), null, link, cancellationToken);
        return await ToResponseAsync(link, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var link = await GetOwnedOrThrowAsync(id, cancellationToken);

        // Un link que ya generó rentas no se puede borrar (violaría la FK de PRT_PortalRentals,
        // que es el comprobante/auditoría del checkout). Desactivarlo logra el mismo efecto
        // de dejar de recibir clientes nuevos, sin perder el historial.
        if (await _repository.HasPortalRentalsAsync(id, cancellationToken))
            throw new AppValidationException(
                "No se puede eliminar un link que ya generó rentas. Desactívalo en su lugar.");

        await _repository.DeleteAsync(id, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Delete, "PRT_PortalLinks", link.Id.ToString(), link, null, cancellationToken);
    }

    private async Task<AssetGroup> GetOwnedAssetGroupOrThrowAsync(Guid landlordId, int assetGroupId, CancellationToken cancellationToken)
    {
        var assetGroup = await _assetGroupRepository.GetByIdAsync(assetGroupId, cancellationToken)
            ?? throw new NotFoundException(nameof(AssetGroup), assetGroupId);

        if (assetGroup.LandlordId != landlordId)
            throw new ForbiddenException("El grupo de activos no pertenece al negocio autenticado.");

        return assetGroup;
    }

    private async Task<PortalLink> GetOwnedOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var link = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(PortalLink), id);

        if (link.LandlordId != landlordId)
            throw new ForbiddenException("El link no pertenece al negocio autenticado.");

        return link;
    }

    private async Task<PortalLinkResponse> ToResponseAsync(PortalLink link, CancellationToken cancellationToken)
    {
        var assetGroup = await _assetGroupRepository.GetByIdAsync(link.AssetGroupId, cancellationToken);
        return new PortalLinkResponse(
            link.Id, link.AssetGroupId, assetGroup?.Name ?? "—", link.Title, link.Slug, link.IsActive, link.CreatedAt);
    }

    /// <summary>Genera un slug legible a partir del título; si ya existe, le agrega un sufijo aleatorio corto.</summary>
    private async Task<string> GenerateUniqueSlugAsync(string title, CancellationToken cancellationToken)
    {
        var baseSlug = Slugify(title);
        var slug = baseSlug;
        var attempts = 0;

        while (await _repository.SlugExistsAsync(slug, cancellationToken))
        {
            attempts++;
            var suffix = Guid.NewGuid().ToString("N")[..6];
            slug = $"{baseSlug}-{suffix}";

            if (attempts > 10)
                throw new AppValidationException("No se pudo generar un identificador único para el link. Intentá de nuevo.");
        }

        return slug;
    }

    private static string Slugify(string title)
    {
        var normalized = title.Trim().ToLowerInvariant();
        var builder = new StringBuilder();
        var lastWasDash = false;

        foreach (var ch in normalized)
        {
            if (char.IsLetterOrDigit(ch) && ch < 128)
            {
                builder.Append(ch);
                lastWasDash = false;
            }
            else if (!lastWasDash && builder.Length > 0)
            {
                builder.Append('-');
                lastWasDash = true;
            }
        }

        var slug = builder.ToString().Trim('-');
        if (string.IsNullOrWhiteSpace(slug))
            slug = "portal";

        return slug.Length > 30 ? slug[..30].Trim('-') : slug;
    }
}
