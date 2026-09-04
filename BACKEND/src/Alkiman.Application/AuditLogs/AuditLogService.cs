using Alkiman.Application.Common.Interfaces;
using Alkiman.Domain.Entities;
using Alkiman.Domain.Enums;

namespace Alkiman.Application.AuditLogs;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _repository;
    private readonly ICurrentLandlordService _currentLandlord;

    public AuditLogService(IAuditLogRepository repository, ICurrentLandlordService currentLandlord)
    {
        _repository = repository;
        _currentLandlord = currentLandlord;
    }

    public async Task<IReadOnlyList<AuditLogResponse>> GetMyActivityAsync(CancellationToken cancellationToken = default)
    {
        var logs = await _repository.GetAllByUserAsync(_currentLandlord.UserId, cancellationToken);
        return logs.Select(ToResponse).ToList();
    }

    public async Task LogAsync(AuditActionType type, string tableName, string primaryKey, object? oldValues, object? newValues, CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = new AuditLog
            {
                UserId = _currentLandlord.UserId,
                Type = type,
                TableName = tableName,
                PrimaryKey = primaryKey,
                OldValues = oldValues is null ? null : System.Text.Json.JsonSerializer.Serialize(oldValues),
                NewValues = newValues is null ? null : System.Text.Json.JsonSerializer.Serialize(newValues),
                Date = DateTime.UtcNow,
            };
            await _repository.CreateAsync(auditLog, cancellationToken);
        }
        catch
        {
            // La bitácora es best-effort: nunca debe romper la operación de negocio que la originó.
        }
    }

    private static AuditLogResponse ToResponse(AuditLog log) => new(
        log.Id, log.UserId, log.Type, log.TableName, log.PrimaryKey, log.OldValues, log.NewValues, log.Date);
}
