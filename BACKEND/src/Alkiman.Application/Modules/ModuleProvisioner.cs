using Alkiman.Application.Common.Roles;
using Alkiman.Application.Permissions;
using Alkiman.Application.Roles;

namespace Alkiman.Application.Modules;

public class ModuleProvisioner : IModuleProvisioner
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;

    public ModuleProvisioner(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
    }

    /// <summary>
    /// Le da al rol Administrador los permisos del módulo.
    ///
    /// Hace falta porque el alta ya no reparte permisos "por si acaso": el registro solo
    /// entrega los de los módulos iniciales, así que sin esto comprar un módulo dejaría
    /// al dueño con el menú visible y 403 en cada request.
    ///
    /// Solo toca al Administrador. Los roles que el negocio armó a mano se quedan como
    /// están: los permisos nuevos se los asigna el dueño desde el editor cuando decida
    /// a quién dárselos.
    ///
    /// Antes esto además sembraba los "roles de sistema" que cada módulo declaraba en un
    /// catálogo (así nacía el rol "Lavador" de Carwash). Se quitó entero junto con el
    /// catálogo: sembrar roles solo le llenaba el mantenimiento al negocio con roles que
    /// no pidió y que, por ser IsSystem, tampoco podía borrar. Ver el script 25.
    /// </summary>
    public async Task ProvisionAsync(Guid landlordId, string moduleCode, CancellationToken cancellationToken = default)
    {
        var roles = await _roleRepository.GetAllByLandlordAsync(landlordId, cancellationToken);
        var owner = roles.FirstOrDefault(r => r.IsSystem && string.Equals(r.Name, SystemRoleNames.Owner, StringComparison.OrdinalIgnoreCase));
        if (owner is null)
            return;

        var permissions = await _permissionRepository.GetAllAsync(cancellationToken);
        var permissionIds = permissions
            .Where(p => string.Equals(p.ModuleCode, moduleCode, StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Id);

        await _roleRepository.AddPermissionsAsync(owner.Id, permissionIds, cancellationToken);
    }
}
