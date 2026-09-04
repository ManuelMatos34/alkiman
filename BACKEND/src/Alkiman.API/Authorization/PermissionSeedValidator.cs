using Alkiman.Application.Common.Permissions;
using Alkiman.Application.Permissions;

namespace Alkiman.API.Authorization;

/// <summary>
/// Verifica que <see cref="PermissionCatalog"/> y <c>dbo.CFG_Permissions</c> digan lo mismo.
///
/// Son dos fuentes que hay que mantener sincronizadas a mano —el catálogo en C# registra
/// las policies, el seed en SQL es lo que se puede asignar a un rol— y desincronizarlas
/// falla raro y tarde:
///   - permiso en el código pero no en la base: existe la policy, pero nadie puede
///     tenerlo nunca, así que el endpoint devuelve 403 para todo el mundo;
///   - permiso en la base pero no en el código: aparece en el editor de roles, se
///     puede asignar, y no protege nada porque no hay policy;
///   - ModuleCode distinto: el permiso se entrega (o se borra) con el módulo equivocado.
///
/// Corre solo en Development, que es donde se agrega un módulo: para cuando llega a
/// producción el desfasaje ya tuvo que aparecer acá. Así un problema de conexión a la
/// base nunca deja el servidor productivo sin arrancar por una verificación de desarrollo.
/// </summary>
public static class PermissionSeedValidator
{
    /// <exception cref="InvalidOperationException">Si el catálogo y el seed no coinciden.</exception>
    public static async Task ValidateAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPermissionRepository>();

        var seeded = (await repository.GetAllAsync(cancellationToken))
            .ToDictionary(p => p.Code, p => p.ModuleCode, StringComparer.OrdinalIgnoreCase);

        var errors = new List<string>();

        foreach (var permission in PermissionCatalog.All)
        {
            if (!seeded.TryGetValue(permission.Code, out var seededModuleCode))
            {
                errors.Add($"'{permission.Code}' está en PermissionCatalog pero falta en CFG_Permissions (falta el INSERT en un script de SCRIPTS/Database).");
                continue;
            }

            if (!string.Equals(seededModuleCode, permission.ModuleCode, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(
                    $"'{permission.Code}' tiene ModuleCode '{permission.ModuleCode ?? "NULL"}' en PermissionCatalog " +
                    $"y '{seededModuleCode ?? "NULL"}' en CFG_Permissions.");
            }
        }

        var catalogCodes = PermissionCatalog.AllCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var code in seeded.Keys.Where(code => !catalogCodes.Contains(code)))
        {
            errors.Add($"'{code}' está en CFG_Permissions pero no en PermissionCatalog: se puede asignar a un rol y no protege ningún endpoint.");
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "PermissionCatalog y CFG_Permissions no coinciden:" + Environment.NewLine + string.Join(Environment.NewLine, errors));
        }
    }
}
