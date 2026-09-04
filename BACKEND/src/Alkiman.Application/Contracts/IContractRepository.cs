using Alkiman.Domain.Entities;

namespace Alkiman.Application.Contracts;

public interface IContractRepository
{
    Task<IReadOnlyList<Contract>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default);
    Task<Contract?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Contract?> GetByRentalIdAsync(Guid rentalId, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(Contract contract, CancellationToken cancellationToken = default);
    Task UpdateAsync(Contract contract, CancellationToken cancellationToken = default);
}
