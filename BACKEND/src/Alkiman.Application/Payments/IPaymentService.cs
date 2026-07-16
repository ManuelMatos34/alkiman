namespace Alkiman.Application.Payments;

public interface IPaymentService
{
    /// <summary>Libro diario de ingresos y egresos del negocio autenticado.</summary>
    Task<IReadOnlyList<PaymentResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PaymentResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaymentResponse> CreateAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default);
}
