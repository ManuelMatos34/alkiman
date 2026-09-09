using Alkiman.Domain.Entities;

namespace Alkiman.Application.Customers;

public interface ICustomerRepository
{
    Task<IReadOnlyList<Customer>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default);
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(Customer customer, CancellationToken cancellationToken = default);
    Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default);
    Task UpdateNameAsync(Guid id, string fullName, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
