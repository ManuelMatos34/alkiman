using Alkiman.Domain.Entities;

namespace Alkiman.Application.Landlords;

public interface ILandlordRepository
{
    Task<Landlord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(Landlord landlord, CancellationToken cancellationToken = default);
    Task UpdateAsync(Landlord landlord, CancellationToken cancellationToken = default);
}
