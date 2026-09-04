using Alkiman.Domain.Enums;

namespace Alkiman.Application.AuditLogs;

public interface IAuditLogService
{
    /// <summary>Bitácora de acciones realizadas por el usuario autenticado.</summary>
    Task<IReadOnlyList<AuditLogResponse>> GetMyActivityAsync(CancellationToken cancellationToken = default);

    /// <summary>Registra una mutación (alta/edición/baja) en la bitácora. Nunca lanza: un fallo al loguear no debe bloquear la operación de negocio que la originó.</summary>
    Task LogAsync(AuditActionType type, string tableName, string primaryKey, object? oldValues, object? newValues, CancellationToken cancellationToken = default);
}
