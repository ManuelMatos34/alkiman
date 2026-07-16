using Alkiman.Application.AssetBlocks;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Domain.Entities;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

public class AssetBlockRepository : IAssetBlockRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AssetBlockRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private const string SelectColumns = """
        Id, AssetId, StartDate, EndDate, Reason, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
        """;

    public async Task<IReadOnlyList<AssetBlock>> GetAllByAssetAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.INV_AssetBlocks
            WHERE AssetId = @AssetId
            ORDER BY StartDate
            """;
        var result = await connection.QueryAsync<AssetBlock>(sql, new { AssetId = assetId });
        return result.ToList();
    }

    public async Task<AssetBlock?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.INV_AssetBlocks
            WHERE Id = @Id
            """;
        return await connection.QuerySingleOrDefaultAsync<AssetBlock>(sql, new { Id = id });
    }

    public async Task<Guid> CreateAsync(AssetBlock block, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO dbo.INV_AssetBlocks (Id, AssetId, StartDate, EndDate, Reason, CreatedAt, CreatedBy)
            VALUES (@Id, @AssetId, @StartDate, @EndDate, @Reason, @CreatedAt, @CreatedBy)
            """;
        await connection.ExecuteAsync(sql, block);
        return block.Id;
    }

    public async Task UpdateAsync(AssetBlock block, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE dbo.INV_AssetBlocks
            SET StartDate = @StartDate,
                EndDate = @EndDate,
                Reason = @Reason,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id
            """;
        await connection.ExecuteAsync(sql, block);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM dbo.INV_AssetBlocks WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }
}
