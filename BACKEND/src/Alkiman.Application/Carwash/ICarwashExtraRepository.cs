using Alkiman.Domain.Entities;

namespace Alkiman.Application.Carwash;

/// <summary>Catálogo de extras (CWS_ServiceExtras). Los extras ya aplicados a un ticket viven en <see cref="ICarwashTicketRepository"/>.</summary>
public interface ICarwashExtraRepository
{
    Task<IReadOnlyList<CarwashServiceExtra>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default);
    Task<CarwashServiceExtra?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(CarwashServiceExtra extra, CancellationToken cancellationToken = default);
    Task UpdateAsync(CarwashServiceExtra extra, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
