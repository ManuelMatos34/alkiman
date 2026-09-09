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

        // El JOIN contra el catálogo filtrando IsAvailable = 1 es lo que convierte esa
        // bandera en un interruptor de verdad: un módulo retirado del catálogo deja de
        // estar habilitado para todos, aunque conserven la fila en CFG_LandlordModules.
        //
        // Sin esto, bajar IsAvailable sólo pintaba el cartel de "Próximamente" en el
        // selector: los negocios que ya lo tenían habilitado seguían entrando por URL
        // directa y la API les seguía respondiendo. Con el filtro acá, la misma consulta
        // cubre las tres puertas (selector, ModuleRoute del front y RequireModule del
        // back), así que sacar o devolver un módulo es cambiar un bit y nada más.
        //
        // La fila de habilitación NO se borra a propósito: cuando el módulo vuelva a
        // estar disponible, cada negocio lo recupera tal como lo tenía.
        const string sql = """
            SELECT lm.ModuleCode
            FROM dbo.CFG_LandlordModules lm
            INNER JOIN dbo.CFG_Modules m ON m.Code = lm.ModuleCode
            WHERE lm.LandlordId = @LandlordId
              AND m.IsAvailable = 1
            """;
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
