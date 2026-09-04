using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Modules;

namespace Alkiman.Application.Permissions;

public class PermissionService : IPermissionService
{
    private readonly IPermissionRepository _repository;
    private readonly IModuleRepository _moduleRepository;
    private readonly ICurrentLandlordService _currentLandlord;

    public PermissionService(
        IPermissionRepository repository,
        IModuleRepository moduleRepository,
        ICurrentLandlordService currentLandlord)
    {
        _repository = repository;
        _moduleRepository = moduleRepository;
        _currentLandlord = currentLandlord;
    }

    /// <summary>
    /// Devuelve solo los permisos que el negocio puede asignar hoy: los de plataforma
    /// (ModuleCode null) más los de los módulos que tiene habilitados. Se filtra acá y
    /// no en el front para que el editor de roles no pueda ofrecer permisos de un módulo
    /// que no se compró.
    /// </summary>
    public async Task<IReadOnlyList<PermissionResponse>> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var enabledModules = await _moduleRepository.GetEnabledModuleCodesAsync(landlordId, cancellationToken);
        var enabled = new HashSet<string>(enabledModules, StringComparer.OrdinalIgnoreCase);

        var permissions = await _repository.GetAllAsync(cancellationToken);
        return permissions
            .Where(p => p.ModuleCode is null || enabled.Contains(p.ModuleCode))
            .Select(p => new PermissionResponse(p.Id, p.Code, p.Module, p.Description, p.ModuleCode))
            .ToList();
    }
}
