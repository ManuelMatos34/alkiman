using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Rentals;
using Alkiman.Domain.Entities;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

public class RentalRepository : IRentalRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RentalRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private const string SelectColumns = """
        r.Id, r.AssetId, r.CustomerId, r.StartDate, r.EndDate, r.ContractPdfUrl, r.TotalPrice,
        r.Status, r.CreatedAt, r.CreatedBy, r.UpdatedAt, r.UpdatedBy
        """;

    public async Task<IReadOnlyList<Rental>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.TRX_Rentals r
            INNER JOIN dbo.INV_Assets a ON a.Id = r.AssetId
            WHERE a.LandlordId = @LandlordId
            ORDER BY r.StartDate DESC
            """;
        var result = await connection.QueryAsync<Rental>(sql, new { LandlordId = landlordId });
        return result.ToList();
    }

    public async Task<IReadOnlyList<Rental>> GetAllByAssetAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.TRX_Rentals r
            WHERE r.AssetId = @AssetId
            ORDER BY r.StartDate DESC
            """;
        var result = await connection.QueryAsync<Rental>(sql, new { AssetId = assetId });
        return result.ToList();
    }

    public async Task<Rental?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.TRX_Rentals r
            WHERE r.Id = @Id
            """;
        return await connection.QuerySingleOrDefaultAsync<Rental>(sql, new { Id = id });
    }

    public async Task<Guid> CreateAsync(Rental rental, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO dbo.TRX_Rentals
                (Id, AssetId, CustomerId, StartDate, EndDate, ContractPdfUrl, TotalPrice, Status, CreatedAt, CreatedBy)
            VALUES
                (@Id, @AssetId, @CustomerId, @StartDate, @EndDate, @ContractPdfUrl, @TotalPrice, @Status, @CreatedAt, @CreatedBy)
            """;
        await connection.ExecuteAsync(sql, rental);
        return rental.Id;
    }

    public async Task UpdateAsync(Rental rental, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE dbo.TRX_Rentals
            SET StartDate = @StartDate,
                EndDate = @EndDate,
                ContractPdfUrl = @ContractPdfUrl,
                TotalPrice = @TotalPrice,
                Status = @Status,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id
            """;
        await connection.ExecuteAsync(sql, rental);
    }
}
