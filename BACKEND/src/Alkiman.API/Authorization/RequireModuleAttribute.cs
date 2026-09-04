using Alkiman.Application.Common.Exceptions;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Modules;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Alkiman.API.Authorization;

/// <summary>
/// Exige que el negocio autenticado tenga habilitado un módulo (CFG_LandlordModules)
/// para poder entrar al controller.
///
/// Es una capa distinta de <c>[Authorize(Policy = ...)]</c> y las dos hacen falta:
/// la policy responde "¿este usuario tiene el permiso?" y este filtro responde
/// "¿este negocio contrató el módulo?". Un permiso viejo que quedó en un rol, o un
/// token emitido antes de que se deshabilitara el módulo, pasan la policy pero no
/// este filtro.
///
/// Se aplica a nivel de controller, junto al <c>[Authorize]</c>: depende del usuario
/// autenticado (lee el claim landlord_id) y por eso NO va en los controllers públicos.
///
/// Devuelve 403 lanzando <see cref="ForbiddenException"/>, que
/// <c>ExceptionHandlingMiddleware</c> —registrado antes de UseAuthorization— convierte
/// en el mismo ProblemDetails que usa el resto de la API.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequireModuleAttribute : Attribute, IAsyncAuthorizationFilter
{
    /// <summary>Módulo exigido (CFG_Modules.Code). Público para que <see cref="ModuleGuardValidator"/> pueda auditar la cobertura al arrancar.</summary>
    public string ModuleCode { get; }

    public RequireModuleAttribute(string moduleCode)
    {
        ModuleCode = moduleCode;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Sin usuario autenticado no hay landlord que consultar: que resuelva el
        // pipeline de autenticación (401) en vez de reportar un 403 confuso.
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var services = context.HttpContext.RequestServices;
        var currentLandlord = services.GetRequiredService<ICurrentLandlordService>();
        var moduleRepository = services.GetRequiredService<IModuleRepository>();

        var cancellationToken = context.HttpContext.RequestAborted;
        var landlordId = await currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var enabledModules = await moduleRepository.GetEnabledModuleCodesAsync(landlordId, cancellationToken);

        if (!enabledModules.Contains(ModuleCode, StringComparer.OrdinalIgnoreCase))
        {
            throw new ForbiddenException($"El módulo '{ModuleCode}' no está habilitado para este negocio.");
        }
    }
}
