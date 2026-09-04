using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Modules;
using Alkiman.Domain.Entities;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

public class ModuleRepository : IModuleRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ModuleRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Module>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT Code, Name, Description, IconName, IsAvailable, SortOrder, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
            FROM dbo.CFG_Modules
            ORDER BY SortOrder
            """;
        var result = await connection.QueryAsync<Module>(sql);
        return result.ToList();
    }

    public async Task<IReadOnlyList<string>> GetEnabledModuleCodesAsync(Guid landlordId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "SELECT ModuleCode FROM dbo.CFG_LandlordModules WHERE LandlordId = @LandlordId";
        var result = await connection.QueryAsync<string>(sql, new { LandlordId = landlordId });
        return result.ToList();
    }

    public async Task EnableModuleAsync(Guid landlordId, string moduleCode, string createdBy, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO dbo.CFG_LandlordModules (LandlordId, ModuleCode, CreatedAt, CreatedBy)
            VALUES (@LandlordId, @ModuleCode, SYSUTCDATETIME(), @CreatedBy)
            """;
        await connection.ExecuteAsync(sql, new { LandlordId = landlordId, ModuleCode = moduleCode, CreatedBy = createdBy });
    }
}
