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
        Id, LandlordId, FullName, Phone, Email, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
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

    public async Task<CarwashWasher?> GetSingleActiveAsync(Guid landlordId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        // TOP 2 y no TOP 1: hace falta poder DISTINGUIR "hay exactamente uno" de
        // "hay varios". Con TOP 1 las dos situaciones devuelven fila y el modo
        // Solitario le terminaría asignando el trabajo a un lavador cualquiera.
        var sql = $"""
            SELECT TOP 2 {SelectColumns}
            FROM dbo.CWS_Washers
            WHERE LandlordId = @LandlordId AND IsActive = 1
            """;
        var rows = (await connection.QueryAsync<CarwashWasher>(sql, new { LandlordId = landlordId })).ToList();
        return rows.Count == 1 ? rows[0] : null;
    }

    public async Task CreateAsync(CarwashWasher washer, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO dbo.CWS_Washers (Id, LandlordId, FullName, Phone, Email, IsActive, CreatedAt, CreatedBy)
            VALUES (@Id, @LandlordId, @FullName, @Phone, @Email, @IsActive, @CreatedAt, @CreatedBy)
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
                Email = @Email,
                IsActive = @IsActive,
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
