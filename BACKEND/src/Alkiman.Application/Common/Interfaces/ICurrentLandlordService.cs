namespace Alkiman.Application.Common.Interfaces;

/// <summary>
/// Resuelve la identidad del propietario (landlord) autenticado actualmente,
/// a partir del claim 'sub' del token JWT de Auth0. Todas las consultas de
/// negocio se filtran por LandlordId para aislar los datos entre propietarios.
/// </summary>
public interface ICurrentLandlordService
{
    /// <summary>Auth0UserId (claim 'sub') del usuario autenticado.</summary>
    string Auth0UserId { get; }

    /// <summary>
    /// Busca el LandlordId asociado al usuario autenticado. Lanza <see cref="Exceptions.NotFoundException"/>
    /// si el usuario aún no completó el registro de su negocio (ver LandlordsController.Register).
    /// </summary>
    Task<Guid> GetCurrentLandlordIdAsync(CancellationToken cancellationToken = default);
}
