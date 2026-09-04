using Alkiman.Domain.Entities;

namespace Alkiman.Application.Modules;

public interface IModuleRepository
{
    Task<IReadOnlyList<Module>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetEnabledModuleCodesAsync(Guid landlordId, CancellationToken cancellationToken = default);
    Task EnableModuleAsync(Guid landlordId, string moduleCode, string createdBy, CancellationToken cancellationToken = default);
}
