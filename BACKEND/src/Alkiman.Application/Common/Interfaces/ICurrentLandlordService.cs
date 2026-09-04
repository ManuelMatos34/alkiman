namespace Alkiman.Application.Common.Interfaces;

/// <summary>
/// Resuelve el aislamiento de datos por negocio (LandlordId) a partir de los
/// claims del JWT propio emitido por la API. Todas las consultas de negocio se
/// filtran por LandlordId para aislar los datos entre negocios (tenants).
/// Desde que el proyecto es multiusuario, el claim 'sub' del token es el
/// UserId (no el LandlordId); el LandlordId viaja en el claim 'landlord_id'.
/// Para datos del usuario autenticado en sí (rol, permisos), ver <see cref="ICurrentUserService"/>.
/// </summary>
public interface ICurrentLandlordService
{
    /// <summary>Id del usuario autenticado (claim 'sub'), como string. Se usa como CreatedBy/UpdatedBy de auditoría.</summary>
    string UserId { get; }

    /// <summary>LandlordId (negocio/tenant) del usuario autenticado (claim 'landlord_id').</summary>
    Task<Guid> GetCurrentLandlordIdAsync(CancellationToken cancellationToken = default);
}
