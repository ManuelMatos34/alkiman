namespace Alkiman.Application.Permissions;

public interface IPermissionService
{
    /// <summary>Catálogo completo de permisos disponibles, para armar el editor de roles en el frontend.</summary>
    Task<IReadOnlyList<PermissionResponse>> GetCatalogAsync(CancellationToken cancellationToken = default);
}
