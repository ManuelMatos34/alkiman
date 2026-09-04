using Alkiman.Domain.Entities;

namespace Alkiman.Application.Portal;

/// <summary>Acceso a datos de los flujos públicos (sin autenticación) del Portal de Rentas.</summary>
public interface IPortalRepository
{
    Task<PortalLink?> GetActiveLinkBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PortalAssetResponse>> GetCatalogAssetsAsync(int assetGroupId, CancellationToken cancellationToken = default);
    Task CreatePortalRentalAsync(PortalRental portalRental, CancellationToken cancellationToken = default);
}
