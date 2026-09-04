using Alkiman.Domain.Entities;

namespace Alkiman.Application.Carwash;

public interface ICarwashServiceRepository
{
    Task<IReadOnlyList<CarwashServiceItem>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default);
    Task<CarwashServiceItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(CarwashServiceItem service, CancellationToken cancellationToken = default);
    Task UpdateAsync(CarwashServiceItem service, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Si el servicio ya tiene tickets asociados, no se puede eliminar (violaría la FK de CWS_Tickets) — hay que desactivarlo en su lugar.</summary>
    Task<bool> HasTicketsAsync(int serviceId, CancellationToken cancellationToken = default);
}
