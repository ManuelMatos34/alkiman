using Alkiman.Domain.Enums;

namespace Alkiman.Application.AuditLogs;

public record AuditLogResponse(
    long Id,
    string UserId,
    AuditActionType Type,
    string TableName,
    string PrimaryKey,
    string? OldValues,
    string? NewValues,
    DateTime Date);
