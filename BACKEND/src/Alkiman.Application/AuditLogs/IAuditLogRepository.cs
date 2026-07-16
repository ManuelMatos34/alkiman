using Alkiman.Domain.Entities;

namespace Alkiman.Application.AuditLogs;

public interface IAuditLogRepository
{
    Task<IReadOnlyList<AuditLog>> GetAllByUserAsync(string userId, CancellationToken cancellationToken = default);
    Task CreateAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
}
