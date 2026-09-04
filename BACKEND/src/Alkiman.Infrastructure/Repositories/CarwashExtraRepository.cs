using Alkiman.Application.Carwash;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Domain.Entities;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

public class CarwashExtraRepository : ICarwashExtraRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CarwashExtraRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private const string SelectColumns = """
        Id, LandlordId, Name, Description, Price, EstimatedMinutes, IsActive,
        CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
        """;

    public async Task<IReadOnlyList<CarwashServiceExtra>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.CWS_ServiceExtras
            WHERE LandlordId = @LandlordId
            ORDER BY Name
            """;
        var result = await connection.QueryAsync<CarwashServiceExtra>(sql, new { LandlordId = landlordId });
        return result.ToList();
    }

    public async Task<CarwashServiceExtra?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.CWS_ServiceExtras
            WHERE Id = @Id
            """;
        return await connection.QuerySingleOrDefaultAsync<CarwashServiceExtra>(sql, new { Id = id });
    }

    public async Task<int> CreateAsync(CarwashServiceExtra extra, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO dbo.CWS_ServiceExtras
                (LandlordId, Name, Description, Price, EstimatedMinutes, IsActive, CreatedAt, CreatedBy)
            VALUES
                (@LandlordId, @Name, @Description, @Price, @EstimatedMinutes, @IsActive, @CreatedAt, @CreatedBy);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;
        return await connection.ExecuteScalarAsync<int>(sql, extra);
    }

    public async Task UpdateAsync(CarwashServiceExtra extra, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE dbo.CWS_ServiceExtras
            SET Name = @Name,
                Description = @Description,
                Price = @Price,
                EstimatedMinutes = @EstimatedMinutes,
                IsActive = @IsActive,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id
            """;
        await connection.ExecuteAsync(sql, extra);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM dbo.CWS_ServiceExtras WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }
}
