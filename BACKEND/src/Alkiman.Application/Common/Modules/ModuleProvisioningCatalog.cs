using Alkiman.Application.Common.Permissions;
using Alkiman.Application.Common.Roles;

namespace Alkiman.Application.Common.Modules;

/// <summary>
/// Un rol de sistema que aporta un módulo.
/// </summary>
/// <param name="Name">Nombre del rol; es su identidad, ver <see cref="SystemRoleNames"/>.</param>
/// <param name="Description">Texto que ve el negocio en el editor de roles.</param>
/// <param name="PermissionCodes">
/// Permisos con los que nace. Deben pertenecer al módulo del plan o ser de plataforma;
/// <c>ModuleProvisioner</c> lo verifica al arrancar la app.
/// </param>
/// <param name="ProvisionOnEnable">
/// true  -> se crea solo, al habilitar el módulo.
/// false -> lo crea el módulo cuando corresponda, porque depende de cómo se configure
///          (el caso de "Lavador": solo tiene sentido en modo Empresa).
/// </param>
public record ModuleSystemRole(
    string Name,
    string Description,
    IReadOnlyList<string> PermissionCodes,
    bool ProvisionOnEnable);

/// <summary>Lo que hay que sembrar cuando un negocio habilita un módulo.</summary>
public record ModuleProvisioningPlan(
    string ModuleCode,
    IReadOnlyList<ModuleSystemRole> SystemRoles);

/// <summary>
/// Declara, por módulo, los roles de sistema que trae consigo.
///
/// Existe para que agregar un módulo sea escribir una entrada acá y no repartir
/// INSERTs por scripts SQL y servicios: así se armó el desastre que limpió el
/// script 18, donde los scripts 16 y 17 le dieron permisos de Carwash a todo rol
/// IsSystem de todo negocio.
///
/// NO incluye los permisos del rol Administrador: esos salen de
/// <see cref="PermissionCatalog"/> filtrando por ModuleCode, así que un permiso
/// nuevo le llega al dueño sin tocar este archivo.
///
/// Un módulo sin roles propios (Alquileres) simplemente no aparece.
/// </summary>
public static class ModuleProvisioningCatalog
{
    public static readonly IReadOnlyList<ModuleProvisioningPlan> All =
    [
        new(ModuleCodes.Carwash,
        [
            new ModuleSystemRole(
                SystemRoleNames.Washer,
                "Puede ver la cola de Carwash y avanzar el estado de los vehículos asignados.",
                [PermissionCodes.CarwashView, PermissionCodes.CarwashWork],
                // A propósito NO lleva carwash.manage: un lavador no debe poder borrar
                // el catálogo de servicios ni los links públicos.
                ProvisionOnEnable: false)
        ])
    ];

    public static ModuleProvisioningPlan? For(string moduleCode) =>
        All.FirstOrDefault(p => string.Equals(p.ModuleCode, moduleCode, StringComparison.OrdinalIgnoreCase));

    public static ModuleSystemRole? FindRole(string moduleCode, string roleName) =>
        For(moduleCode)?.SystemRoles
            .FirstOrDefault(r => string.Equals(r.Name, roleName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Verifica que el catálogo sea coherente con <see cref="PermissionCatalog"/>.
    /// La llama Program.cs al arrancar: un código mal escrito revienta el boot en vez
    /// de crear meses después un rol al que le falta un permiso en silencio.
    /// </summary>
    /// <exception cref="InvalidOperationException">Si el catálogo es inconsistente.</exception>
    public static void Validate()
    {
        var errors = new List<string>();

        var duplicatedPlans = All.GroupBy(p => p.ModuleCode, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);
        errors.AddRange(duplicatedPlans.Select(code => $"El módulo '{code}' tiene más de un plan."));

        var byCode = PermissionCatalog.All.ToDictionary(p => p.Code, StringComparer.OrdinalIgnoreCase);

        foreach (var plan in All)
        {
            var duplicatedRoles = plan.SystemRoles.GroupBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);
            errors.AddRange(duplicatedRoles.Select(name => $"[{plan.ModuleCode}] el rol '{name}' está declarado más de una vez."));

            foreach (var role in plan.SystemRoles)
            {
                if (role.PermissionCodes.Count == 0)
                    errors.Add($"[{plan.ModuleCode}] el rol '{role.Name}' no declara permisos.");

                foreach (var code in role.PermissionCodes)
                {
                    if (!byCode.TryGetValue(code, out var permission))
                    {
                        errors.Add($"[{plan.ModuleCode}] el rol '{role.Name}' declara el permiso '{code}', que no existe en PermissionCatalog.");
                        continue;
                    }

                    // Un rol de módulo puede llevar permisos de plataforma (ModuleCode null),
                    // pero nunca de OTRO módulo: ese negocio podría no haberlo comprado.
                    if (permission.ModuleCode is not null &&
                        !string.Equals(permission.ModuleCode, plan.ModuleCode, StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add($"[{plan.ModuleCode}] el rol '{role.Name}' declara el permiso '{code}', que pertenece al módulo '{permission.ModuleCode}'.");
                    }
                }
            }
        }

        if (errors.Count > 0)
            throw new InvalidOperationException("ModuleProvisioningCatalog inválido:" + Environment.NewLine + string.Join(Environment.NewLine, errors));
    }
}
