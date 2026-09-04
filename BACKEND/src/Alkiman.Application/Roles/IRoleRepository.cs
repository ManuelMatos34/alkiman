using Alkiman.Domain.Entities;

namespace Alkiman.Application.Roles;

public interface IRoleRepository
{
    Task<IReadOnlyList<Role>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default);
    Task<Role?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetPermissionCodesAsync(int roleId, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(Role role, IEnumerable<int> permissionIds, CancellationToken cancellationToken = default);
    Task UpdateAsync(Role role, IEnumerable<int> permissionIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Agrega permisos a un rol sin tocar los que ya tiene (a diferencia de
    /// <see cref="UpdateAsync"/>, que reemplaza el set completo). Lo usa el alta de
    /// módulos para dar los permisos nuevos al rol Administrador. Es idempotente.
    /// </summary>
    Task AddPermissionsAsync(int roleId, IEnumerable<int> permissionIds, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CountUsersAsync(int roleId, CancellationToken cancellationToken = default);
}
