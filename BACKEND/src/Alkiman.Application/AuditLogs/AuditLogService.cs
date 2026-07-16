using Alkiman.Application.Common.Interfaces;
using Alkiman.Domain.Entities;

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
        var logs = await _repository.GetAllByUserAsync(_currentLandlord.Auth0UserId, cancellationToken);
        return logs.Select(ToResponse).ToList();
    }

    private static AuditLogResponse ToResponse(AuditLog log) => new(
        log.Id, log.UserId, log.Type, log.TableName, log.PrimaryKey, log.OldValues, log.NewValues, log.Date);
}
