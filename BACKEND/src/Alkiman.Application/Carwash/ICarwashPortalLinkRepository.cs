using Alkiman.Domain.Entities;

namespace Alkiman.Application.Carwash;

public interface ICarwashPortalLinkRepository
{
    Task<IReadOnlyList<CarwashPortalLink>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default);
    Task<CarwashPortalLink?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Resuelve el link para el flujo público: solo devuelve el link si existe (independiente de IsActive; el llamador decide si lo rechaza por inactivo).</summary>
    Task<CarwashPortalLink?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(CarwashPortalLink link, CancellationToken cancellationToken = default);
    Task UpdateAsync(CarwashPortalLink link, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>true si algún ticket fue originado por este link (Source=Portal y AccessToken asociado). Impide borrar links que tienen historial.</summary>
    Task<bool> HasTicketsAsync(Guid id, CancellationToken cancellationToken = default);
}
