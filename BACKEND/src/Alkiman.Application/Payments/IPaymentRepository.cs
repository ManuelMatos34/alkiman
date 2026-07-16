using Alkiman.Domain.Entities;

namespace Alkiman.Application.Payments;

public interface IPaymentRepository
{
    Task<IReadOnlyList<Payment>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default);
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(Payment payment, CancellationToken cancellationToken = default);
}
