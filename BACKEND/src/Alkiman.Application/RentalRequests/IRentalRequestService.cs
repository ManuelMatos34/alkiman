namespace Alkiman.Application.RentalRequests;

/// <summary>Casos de uso autenticados (panel) para revisar los pedidos de prórroga/cancelación que los clientes hacen desde su link público "mi-renta".</summary>
public interface IRentalRequestService
{
    Task<IReadOnlyList<RentalRequestResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<RentalRequestResponse> ApproveAsync(Guid id, ReviewRentalRequestRequest request, CancellationToken cancellationToken = default);
    Task<RentalRequestResponse> RejectAsync(Guid id, ReviewRentalRequestRequest request, CancellationToken cancellationToken = default);
}
