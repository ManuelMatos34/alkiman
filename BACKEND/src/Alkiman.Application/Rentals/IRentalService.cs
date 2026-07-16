namespace Alkiman.Application.Rentals;

public interface IRentalService
{
    /// <summary>Todas las rentas de los activos del negocio autenticado (tablero de calendario).</summary>
    Task<IReadOnlyList<RentalResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<RentalResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Crea la renta y, como parte del "happy path" (RF-B2/flujo esencial),
    /// marca el activo como Rentado automáticamente.
    /// </summary>
    Task<RentalResponse> CreateAsync(CreateRentalRequest request, CancellationToken cancellationToken = default);

    Task<RentalResponse> AttachContractAsync(Guid id, UpdateRentalContractRequest request, CancellationToken cancellationToken = default);
    Task<RentalResponse> UpdateStatusAsync(Guid id, UpdateRentalStatusRequest request, CancellationToken cancellationToken = default);
}
