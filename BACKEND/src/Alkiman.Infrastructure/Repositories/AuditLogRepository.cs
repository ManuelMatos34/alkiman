using Alkiman.Application.AuditLogs;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Domain.Entities;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AuditLogRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<AuditLog>> GetAllByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT Id, UserId, Type, TableName, PrimaryKey, OldValues, NewValues, Date
            FROM dbo.AUD_AuditLogs
            WHERE UserId = @UserId
            ORDER BY Date DESC
            """;
        var result = await connection.QueryAsync<AuditLog>(sql, new { UserId = userId });
        return result.ToList();
    }

    public async Task CreateAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO dbo.AUD_AuditLogs (UserId, Type, TableName, PrimaryKey, OldValues, NewValues, Date)
            VALUES (@UserId, @Type, @TableName, @PrimaryKey, @OldValues, @NewValues, @Date)
            """;
        await connection.ExecuteAsync(sql, auditLog);
    }
}
