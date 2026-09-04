namespace Alkiman.Application.Common.Interfaces;

/// <summary>
/// Resuelve la identidad completa (usuario, rol y permisos) del usuario
/// autenticado actualmente, a partir de los claims del JWT propio.
/// Complementa a <see cref="ICurrentLandlordService"/>, que solo resuelve
/// el aislamiento de datos por negocio (LandlordId).
/// </summary>
public interface ICurrentUserService
{
    /// <summary>Id del usuario autenticado (claim 'sub').</summary>
    Guid UserId { get; }

    /// <summary>Negocio (tenant) al que pertenece el usuario autenticado.</summary>
    Guid LandlordId { get; }

    /// <summary>Nombre del rol asignado al usuario autenticado.</summary>
    string Role { get; }

    /// <summary>Indica si el usuario autenticado es el dueño del negocio.</summary>
    bool IsOwner { get; }

    /// <summary>Códigos de permiso otorgados por el rol del usuario autenticado.</summary>
    IReadOnlyList<string> Permissions { get; }

    /// <summary>Indica si el usuario autenticado tiene el permiso indicado.</summary>
    bool HasPermission(string code);
}
