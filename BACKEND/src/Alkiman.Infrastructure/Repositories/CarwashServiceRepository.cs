using Alkiman.Application.Carwash;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Domain.Entities;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

public class CarwashServiceRepository : ICarwashServiceRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CarwashServiceRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private const string SelectColumns = """
        Id, LandlordId, Name, Description, Price, EstimatedMinutes, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
        """;

    public async Task<IReadOnlyList<CarwashServiceItem>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.CWS_Services
            WHERE LandlordId = @LandlordId
            ORDER BY Name
            """;
        var result = await connection.QueryAsync<CarwashServiceItem>(sql, new { LandlordId = landlordId });
        return result.ToList();
    }

    public async Task<CarwashServiceItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.CWS_Services
            WHERE Id = @Id
            """;
        return await connection.QuerySingleOrDefaultAsync<CarwashServiceItem>(sql, new { Id = id });
    }

    public async Task<int> CreateAsync(CarwashServiceItem service, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO dbo.CWS_Services (LandlordId, Name, Description, Price, EstimatedMinutes, IsActive, CreatedAt, CreatedBy)
            OUTPUT INSERTED.Id
            VALUES (@LandlordId, @Name, @Description, @Price, @EstimatedMinutes, @IsActive, @CreatedAt, @CreatedBy)
            """;
        return await connection.ExecuteScalarAsync<int>(sql, service);
    }

    public async Task UpdateAsync(CarwashServiceItem service, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE dbo.CWS_Services
            SET Name = @Name,
                Description = @Description,
                Price = @Price,
                EstimatedMinutes = @EstimatedMinutes,
                IsActive = @IsActive,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id
            """;
        await connection.ExecuteAsync(sql, service);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM dbo.CWS_Services WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<bool> HasTicketsAsync(int serviceId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "SELECT COUNT(1) FROM dbo.CWS_Tickets WHERE ServiceId = @ServiceId";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { ServiceId = serviceId });
        return count > 0;
    }
}
