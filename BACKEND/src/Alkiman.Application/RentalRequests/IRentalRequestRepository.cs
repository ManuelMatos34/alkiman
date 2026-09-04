using Alkiman.Domain.Entities;

namespace Alkiman.Application.RentalRequests;

public interface IRentalRequestRepository
{
    Task<IReadOnlyList<RentalRequest>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default);
    Task<RentalRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RentalRequest?> GetPendingByRentalAsync(Guid rentalId, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(RentalRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(RentalRequest request, CancellationToken cancellationToken = default);
}
