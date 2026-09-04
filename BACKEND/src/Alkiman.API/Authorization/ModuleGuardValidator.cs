using System.Reflection;
using Alkiman.Application.Common.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alkiman.API.Authorization;

/// <summary>
/// Audita, al arrancar, que todo endpoint que exige un permiso de módulo esté además
/// cubierto por <see cref="RequireModuleAttribute"/>.
///
/// Existe porque las dos capas se ponen a mano y responden preguntas distintas: la
/// policy mira el permiso del usuario, el filtro mira si el negocio compró el módulo.
/// Olvidarse del filtro en el módulo N+1 no rompe nada visible —los tests pasan, la
/// pantalla anda— pero deja el módulo accesible a negocios que no lo contrataron, y
/// no hay forma de notarlo hasta que alguien lo explote.
///
/// Es el mismo criterio que <c>ModuleProvisioningCatalog.Validate()</c>: mejor no
/// levantar que arrastrar un agujero en silencio.
/// </summary>
public static class ModuleGuardValidator
{
    /// <exception cref="InvalidOperationException">Si algún endpoint quedó sin cubrir.</exception>
    public static void Validate(Assembly assembly)
    {
        // Solo interesan los permisos que pertenecen a un módulo: los de plataforma
        // (ModuleCode null) existen sin importar qué compró el negocio.
        var moduleByPermission = PermissionCatalog.All
            .Where(p => p.ModuleCode is not null)
            .ToDictionary(p => p.Code, p => p.ModuleCode!, StringComparer.OrdinalIgnoreCase);

        var errors = new List<string>();

        var controllers = assembly.GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract);

        foreach (var controller in controllers)
        {
            var controllerGuard = controller.GetCustomAttribute<RequireModuleAttribute>()?.ModuleCode;
            var controllerPolicies = PoliciesOf(controller.GetCustomAttributes<AuthorizeAttribute>());

            foreach (var action in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (action.IsSpecialName || action.GetCustomAttribute<NonActionAttribute>() is not null)
                    continue;

                // Una acción anónima no pasa por las policies del controller, así que
                // tampoco arrastra la exigencia de módulo (caso de los portales públicos).
                if (action.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
                    continue;

                var guard = action.GetCustomAttribute<RequireModuleAttribute>()?.ModuleCode ?? controllerGuard;

                var requiredModules = controllerPolicies
                    .Concat(PoliciesOf(action.GetCustomAttributes<AuthorizeAttribute>()))
                    .Where(moduleByPermission.ContainsKey)
                    .Select(policy => moduleByPermission[policy])
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var moduleCode in requiredModules)
                {
                    if (guard is null)
                    {
                        errors.Add(
                            $"{controller.Name}.{action.Name} exige un permiso del módulo '{moduleCode}' " +
                            $"pero no lleva [RequireModule]: un negocio que no compró el módulo puede entrar.");
                    }
                    else if (!string.Equals(guard, moduleCode, StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add(
                            $"{controller.Name}.{action.Name} lleva [RequireModule(\"{guard}\")] " +
                            $"pero exige un permiso del módulo '{moduleCode}'.");
                    }
                }
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Endpoints sin guard de módulo:" + Environment.NewLine + string.Join(Environment.NewLine, errors));
        }
    }

    private static List<string> PoliciesOf(IEnumerable<AuthorizeAttribute> attributes) =>
        attributes
            .Select(attribute => attribute.Policy)
            .Where(policy => !string.IsNullOrWhiteSpace(policy))
            .Select(policy => policy!)
            .ToList();
}
