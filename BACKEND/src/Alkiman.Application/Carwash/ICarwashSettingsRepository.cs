using Alkiman.Domain.Entities;

namespace Alkiman.Application.Carwash;

/// <summary>
/// Configuración del módulo por negocio (CWS_Settings). Devolver <c>null</c>
/// en <see cref="GetByLandlordAsync"/> significa que el negocio todavía no
/// eligió modo de operación: es lo que dispara el diálogo inicial.
/// </summary>
public interface ICarwashSettingsRepository
{
    Task<CarwashSettings?> GetByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default);

    /// <summary>Inserta la configuración o actualiza el modo si ya existía (la PK es LandlordId, hay a lo sumo una fila por negocio).</summary>
    Task UpsertAsync(CarwashSettings settings, CancellationToken cancellationToken = default);
}
