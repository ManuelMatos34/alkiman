using Alkiman.Domain.Entities;

namespace Alkiman.Application.PortalLinks;

public interface IPortalLinkRepository
{
    Task<IReadOnlyList<PortalLink>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default);
    Task<PortalLink?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>True si el link ya generó al menos una renta (PRT_PortalRentals); se usa para bloquear el borrado y evitar violar la FK.</summary>
    Task<bool> HasPortalRentalsAsync(Guid portalLinkId, CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(PortalLink link, CancellationToken cancellationToken = default);
    Task UpdateAsync(PortalLink link, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
