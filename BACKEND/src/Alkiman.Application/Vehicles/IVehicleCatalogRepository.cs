using Alkiman.Domain.Entities;

namespace Alkiman.Application.Vehicles;

public interface IVehicleCatalogRepository
{
    Task<IReadOnlyList<VehicleMake>> GetMakesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VehicleModel>> GetModelsByMakeAsync(int makeId, CancellationToken cancellationToken = default);
}
