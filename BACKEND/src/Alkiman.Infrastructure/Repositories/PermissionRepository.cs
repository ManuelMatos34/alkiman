using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Permissions;
using Alkiman.Domain.Entities;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

public class PermissionRepository : IPermissionRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PermissionRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT Id, Code, Module, ModuleCode, Description
            FROM dbo.CFG_Permissions
            ORDER BY Module, Code
            """;
        var result = await connection.QueryAsync<Permission>(sql);
        return result.ToList();
    }
}
