using Alkiman.Domain.Enums;

namespace Alkiman.Domain.Entities;

/// <summary>Bitácora de eventos globales del sistema. Tabla: AUD_AuditLogs.</summary>
public class AuditLog
{
    public long Id { get; set; }
    public string UserId { get; set; } = default!;
    public AuditActionType Type { get; set; }
    public string TableName { get; set; } = default!;
    public string PrimaryKey { get; set; } = default!;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public DateTime Date { get; set; }
}
