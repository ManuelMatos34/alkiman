using Alkiman.Application.Carwash;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Domain.Entities;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

public class CarwashWasherRepository : ICarwashWasherRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CarwashWasherRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private const string SelectColumns = """
        Id, LandlordId, FullName, Phone, IsActive, UserId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
        """;

    public async Task<IReadOnlyList<CarwashWasher>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.CWS_Washers
            WHERE LandlordId = @LandlordId
            ORDER BY FullName
            """;
        var result = await connection.QueryAsync<CarwashWasher>(sql, new { LandlordId = landlordId });
        return result.ToList();
    }

    public async Task<CarwashWasher?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.CWS_Washers
            WHERE Id = @Id
            """;
        return await connection.QuerySingleOrDefaultAsync<CarwashWasher>(sql, new { Id = id });
    }

    public async Task<CarwashWasher?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.CWS_Washers
            WHERE UserId = @UserId
            """;
        return await connection.QuerySingleOrDefaultAsync<CarwashWasher>(sql, new { UserId = userId });
    }

    public async Task CreateAsync(CarwashWasher washer, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO dbo.CWS_Washers (Id, LandlordId, FullName, Phone, IsActive, UserId, CreatedAt, CreatedBy)
            VALUES (@Id, @LandlordId, @FullName, @Phone, @IsActive, @UserId, @CreatedAt, @CreatedBy)
            """;
        await connection.ExecuteAsync(sql, washer);
    }

    public async Task UpdateAsync(CarwashWasher washer, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE dbo.CWS_Washers
            SET FullName = @FullName,
                Phone = @Phone,
                IsActive = @IsActive,
                UserId = @UserId,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id
            """;
        await connection.ExecuteAsync(sql, washer);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM dbo.CWS_Washers WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<bool> HasTicketsAsync(Guid washerId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "SELECT COUNT(1) FROM dbo.CWS_Tickets WHERE AssignedToWasherId = @WasherId";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { WasherId = washerId });
        return count > 0;
    }
}
