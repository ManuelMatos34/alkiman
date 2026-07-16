using Alkiman.Application.Assets;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Domain.Entities;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

public class AssetRepository : IAssetRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AssetRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private const string SelectColumns = """
        Id, LandlordId, CategoryId, Name, Description, ImageUrl, Status, RentalType,
        BasePrice, Stock, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
        """;

    public async Task<IReadOnlyList<Asset>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.INV_Assets
            WHERE LandlordId = @LandlordId
            ORDER BY Name
            """;
        var result = await connection.QueryAsync<Asset>(sql, new { LandlordId = landlordId });
        return result.ToList();
    }

    public async Task<Asset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.INV_Assets
            WHERE Id = @Id
            """;
        return await connection.QuerySingleOrDefaultAsync<Asset>(sql, new { Id = id });
    }

    public async Task<Guid> CreateAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO dbo.INV_Assets
                (Id, LandlordId, CategoryId, Name, Description, ImageUrl, Status, RentalType, BasePrice, Stock, CreatedAt, CreatedBy)
            VALUES
                (@Id, @LandlordId, @CategoryId, @Name, @Description, @ImageUrl, @Status, @RentalType, @BasePrice, @Stock, @CreatedAt, @CreatedBy)
            """;
        await connection.ExecuteAsync(sql, asset);
        return asset.Id;
    }

    public async Task UpdateAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE dbo.INV_Assets
            SET CategoryId = @CategoryId,
                Name = @Name,
                Description = @Description,
                ImageUrl = @ImageUrl,
                Status = @Status,
                BasePrice = @BasePrice,
                Stock = @Stock,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id
            """;
        await connection.ExecuteAsync(sql, asset);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM dbo.INV_Assets WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }
}
