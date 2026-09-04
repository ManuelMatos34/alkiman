using Alkiman.Domain.Entities;

namespace Alkiman.Application.Rentals;

public interface IRentalRepository
{
    /// <summary>Rentas de todos los activos del landlord (para el tablero de calendario).</summary>
    Task<IReadOnlyList<Rental>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Rental>> GetAllByAssetAsync(Guid assetId, CancellationToken cancellationToken = default);
    Task<Rental?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Rental?> GetByAccessTokenAsync(Guid accessToken, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(Rental rental, CancellationToken cancellationToken = default);
    Task UpdateAsync(Rental rental, CancellationToken cancellationToken = default);
    /// <summary>Update narrow, sin tocar auditoría (UpdatedAt/UpdatedBy): usado solo por la verificación del link público para registrar intentos fallidos/reset del lockout.</summary>
    Task UpdateAccessStateAsync(Guid rentalId, int accessFailedAttempts, DateTime? accessLockedUntil, CancellationToken cancellationToken = default);

    /// <summary>Rentas activas de TODOS los landlords cuyo EndDate cae exactamente en <paramref name="dueDate"/> (comparación por fecha, sin hora); usado por el job de recordatorios automáticos de vencimiento.</summary>
    Task<IReadOnlyList<RentalDueSummary>> GetActiveRentalsDueOnAsync(DateTime dueDate, CancellationToken cancellationToken = default);
}

/// <summary>Resumen liviano de una renta activa próxima a vencer, usado por <see cref="IRentalRepository.GetActiveRentalsDueOnAsync"/> para generar recordatorios automáticos.</summary>
public record RentalDueSummary(Guid RentalId, Guid LandlordId, Guid CustomerId, string AssetName, DateTime EndDate, decimal TotalPrice);
