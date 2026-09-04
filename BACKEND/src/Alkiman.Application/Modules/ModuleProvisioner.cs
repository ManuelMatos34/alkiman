using Alkiman.Application.AuditLogs;
using Alkiman.Application.Common.Modules;
using Alkiman.Application.Common.Roles;
using Alkiman.Application.Permissions;
using Alkiman.Application.Roles;
using Alkiman.Domain.Entities;
using Alkiman.Domain.Enums;

namespace Alkiman.Application.Modules;

public class ModuleProvisioner : IModuleProvisioner
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IAuditLogService _auditLog;

    public ModuleProvisioner(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IAuditLogService auditLog)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _auditLog = auditLog;
    }

    public async Task ProvisionAsync(Guid landlordId, string moduleCode, string createdBy, CancellationToken cancellationToken = default)
    {
        await GrantModulePermissionsToOwnerAsync(landlordId, moduleCode, cancellationToken);

        var plan = ModuleProvisioningCatalog.For(moduleCode);
        if (plan is null)
            return;

        foreach (var role in plan.SystemRoles.Where(r => r.ProvisionOnEnable))
        {
            await EnsureSystemRoleAsync(landlordId, moduleCode, role.Name, createdBy, cancellationToken);
        }
    }

    /// <summary>
    /// Da al rol Administrador los permisos del módulo.
    ///
    /// Hace falta porque el alta ya no reparte permisos "por si acaso": el registro solo
    /// entrega los de los módulos iniciales, así que sin esto comprar un módulo dejaría
    /// al dueño con el menú visible y 403 en cada request.
    ///
    /// Solo toca al Administrador. Los roles que el negocio armó a mano se quedan como
    /// están: los permisos nuevos se los asigna el dueño desde el editor cuando decida
    /// a quién dárselos.
    /// </summary>
    private async Task GrantModulePermissionsToOwnerAsync(Guid landlordId, string moduleCode, CancellationToken cancellationToken)
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

    public async Task EnsureSystemRoleAsync(Guid landlordId, string moduleCode, string roleName, string createdBy, CancellationToken cancellationToken = default)
    {
        var definition = ModuleProvisioningCatalog.FindRole(moduleCode, roleName)
            ?? throw new InvalidOperationException(
                $"El módulo '{moduleCode}' no declara un rol de sistema '{roleName}' en ModuleProvisioningCatalog.");

        var roles = await _roleRepository.GetAllByLandlordAsync(landlordId, cancellationToken);
        if (roles.Any(r => string.Equals(r.Name, definition.Name, StringComparison.OrdinalIgnoreCase)))
            return;

        // Se resuelven contra la BD y no contra el catálogo estático para no crear el
        // rol con permisos que todavía no existen como fila en CFG_Permissions.
        var permissions = await _permissionRepository.GetAllAsync(cancellationToken);
        var permissionIds = permissions
            .Where(p => definition.PermissionCodes.Contains(p.Code, StringComparer.OrdinalIgnoreCase))
            .Select(p => p.Id)
            .ToList();

        var role = new Role
        {
            LandlordId = landlordId,
            Name = definition.Name,
            Description = definition.Description,
            IsSystem = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        role.Id = await _roleRepository.CreateAsync(role, permissionIds, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Create, "CFG_Roles", role.Id.ToString(), null, role, cancellationToken);
    }
}
