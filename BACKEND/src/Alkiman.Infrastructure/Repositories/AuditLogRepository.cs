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
        // Nota: Dapper convierte los enums a su tipo subyacente (int) en LookupDbType
        // *antes* de consultar los TypeHandler registrados, por lo que un TypeHandler<T>
        // para un enum nunca se aplica cuando se pasa la entidad completa como parámetros.
        // Convertimos a texto explícitamente para evitar violar los CHECK constraints.
        await connection.ExecuteAsync(sql, new
        {
            auditLog.UserId,
            Type = auditLog.Type.ToString(),
            auditLog.TableName,
            auditLog.PrimaryKey,
            auditLog.OldValues,
            auditLog.NewValues,
            auditLog.Date
        });
    }
}
