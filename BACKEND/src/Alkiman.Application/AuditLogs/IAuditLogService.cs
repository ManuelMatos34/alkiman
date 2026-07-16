namespace Alkiman.Application.AuditLogs;

public interface IAuditLogService
{
    /// <summary>Bitácora de acciones realizadas por el usuario autenticado.</summary>
    Task<IReadOnlyList<AuditLogResponse>> GetMyActivityAsync(CancellationToken cancellationToken = default);
}
