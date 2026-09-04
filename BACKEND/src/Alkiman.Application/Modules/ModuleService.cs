using Alkiman.Application.Common.Exceptions;
using Alkiman.Application.Common.Interfaces;

namespace Alkiman.Application.Modules;

public class ModuleService : IModuleService
{
    private readonly IModuleRepository _repository;
    private readonly IModuleProvisioner _provisioner;
    private readonly ICurrentLandlordService _currentLandlord;

    public ModuleService(
        IModuleRepository repository,
        IModuleProvisioner provisioner,
        ICurrentLandlordService currentLandlord)
    {
        _repository = repository;
        _provisioner = provisioner;
        _currentLandlord = currentLandlord;
    }

    public async Task<IReadOnlyList<ModuleResponse>> GetAllForCurrentLandlordAsync(CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var modules = await _repository.GetAllAsync(cancellationToken);
        var enabledCodes = (await _repository.GetEnabledModuleCodesAsync(landlordId, cancellationToken)).ToHashSet();

        return modules
            .OrderBy(m => m.SortOrder)
            .Select(m => new ModuleResponse(m.Code, m.Name, m.Description, m.IconName, m.IsAvailable, enabledCodes.Contains(m.Code)))
            .ToList();
    }

    public async Task EnableModuleAsync(string moduleCode, CancellationToken cancellationToken = default)
    {
        var modules = await _repository.GetAllAsync(cancellationToken);
        var module = modules.FirstOrDefault(m => m.Code == moduleCode)
            ?? throw new AppValidationException($"El módulo '{moduleCode}' no existe.");

        if (!module.IsAvailable)
            throw new AppValidationException($"El módulo '{moduleCode}' todavía no está disponible.");

        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var enabledCodes = await _repository.GetEnabledModuleCodesAsync(landlordId, cancellationToken);

        // Idempotente: si ya estaba habilitado, no reintenta el insert (violaría
        // UQ_CFG_LandlordModules_Landlord_Module).
        if (enabledCodes.Contains(moduleCode))
            return;

        await _repository.EnableModuleAsync(landlordId, moduleCode, _currentLandlord.UserId, cancellationToken);
        await _provisioner.ProvisionAsync(landlordId, moduleCode, _currentLandlord.UserId, cancellationToken);
    }
}
