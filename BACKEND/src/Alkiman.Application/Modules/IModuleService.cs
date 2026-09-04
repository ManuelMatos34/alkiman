namespace Alkiman.Application.Modules;

public interface IModuleService
{
    Task<IReadOnlyList<ModuleResponse>> GetAllForCurrentLandlordAsync(CancellationToken cancellationToken = default);

    /// <summary>Habilita un módulo (del catálogo, con IsAvailable=1) para el negocio autenticado. Idempotente: si ya estaba habilitado, no falla.</summary>
    Task EnableModuleAsync(string moduleCode, CancellationToken cancellationToken = default);
}
