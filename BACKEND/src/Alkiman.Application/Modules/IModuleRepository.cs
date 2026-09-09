using Alkiman.Domain.Entities;

namespace Alkiman.Application.Modules;

public interface IModuleRepository
{
    Task<IReadOnlyList<Module>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Módulos habilitados para el negocio y todavía disponibles en el catálogo.
    /// Un módulo con <c>CFG_Modules.IsAvailable = 0</c> NO aparece acá aunque el
    /// negocio tenga la fila de habilitación: retirarlo del catálogo lo cierra para
    /// todos (ver el comentario del SQL en <c>ModuleRepository</c>).
    /// </summary>
    Task<IReadOnlyList<string>> GetEnabledModuleCodesAsync(Guid landlordId, CancellationToken cancellationToken = default);
    Task EnableModuleAsync(Guid landlordId, string moduleCode, string createdBy, CancellationToken cancellationToken = default);
}
