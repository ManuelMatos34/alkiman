using Alkiman.Domain.Entities;

namespace Alkiman.Application.Permissions;

public interface IPermissionRepository
{
    Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken cancellationToken = default);
}
