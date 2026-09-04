using Alkiman.Application.AssetGroups;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Domain.Entities;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

public class AssetGroupRepository : IAssetGroupRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AssetGroupRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<AssetGroup>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT Id, LandlordId, Name, Description, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
            FROM dbo.INV_AssetGroups
            WHERE LandlordId = @LandlordId
            ORDER BY Name
            """;
        var result = await connection.QueryAsync<AssetGroup>(sql, new { LandlordId = landlordId });
        return result.ToList();
    }

    public async Task<AssetGroup?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT Id, LandlordId, Name, Description, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
            FROM dbo.INV_AssetGroups
            WHERE Id = @Id
            """;
        return await connection.QuerySingleOrDefaultAsync<AssetGroup>(sql, new { Id = id });
    }

    public async Task<bool> NameExistsAsync(Guid landlordId, string name, int? excludeId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.INV_AssetGroups
            WHERE LandlordId = @LandlordId
              AND Name = @Name
              AND (@ExcludeId IS NULL OR Id <> @ExcludeId)
            """;
        var count = await connection.ExecuteScalarAsync<int>(sql, new { LandlordId = landlordId, Name = name, ExcludeId = excludeId });
        return count > 0;
    }

    public async Task<IReadOnlyList<Guid>> GetMemberAssetIdsAsync(int groupId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT AssetId
            FROM dbo.INV_AssetGroupAssets
            WHERE AssetGroupId = @GroupId
            """;
        var result = await connection.QueryAsync<Guid>(sql, new { GroupId = groupId });
        return result.ToList();
    }

    public async Task<IReadOnlyList<Guid>> FilterOwnedAssetIdsAsync(Guid landlordId, IEnumerable<Guid> assetIds, CancellationToken cancellationToken = default)
    {
        var ids = assetIds.ToList();
        if (ids.Count == 0)
            return Array.Empty<Guid>();

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT Id
            FROM dbo.INV_Assets
            WHERE LandlordId = @LandlordId AND Id IN @Ids
            """;
        var result = await connection.QueryAsync<Guid>(sql, new { LandlordId = landlordId, Ids = ids });
        return result.ToList();
    }

    public async Task<int> CreateAsync(AssetGroup group, IEnumerable<Guid> assetIds, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        const string insertGroupSql = """
            INSERT INTO dbo.INV_AssetGroups (LandlordId, Name, Description, CreatedAt, CreatedBy)
            OUTPUT INSERTED.Id
            VALUES (@LandlordId, @Name, @Description, @CreatedAt, @CreatedBy)
            """;
        var groupId = await connection.ExecuteScalarAsync<int>(insertGroupSql, group, transaction);

        var rows = assetIds.Select(assetId => new { AssetGroupId = groupId, AssetId = assetId }).ToList();
        if (rows.Count > 0)
        {
            const string insertMemberSql = """
                INSERT INTO dbo.INV_AssetGroupAssets (AssetGroupId, AssetId) VALUES (@AssetGroupId, @AssetId)
                """;
            await connection.ExecuteAsync(insertMemberSql, rows, transaction);
        }

        transaction.Commit();
        return groupId;
    }

    public async Task UpdateAsync(AssetGroup group, IEnumerable<Guid> assetIds, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        const string updateGroupSql = """
            UPDATE dbo.INV_AssetGroups
            SET Name = @Name,
                Description = @Description,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id
            """;
        await connection.ExecuteAsync(updateGroupSql, group, transaction);

        const string deleteMembersSql = "DELETE FROM dbo.INV_AssetGroupAssets WHERE AssetGroupId = @AssetGroupId";
        await connection.ExecuteAsync(deleteMembersSql, new { AssetGroupId = group.Id }, transaction);

        var rows = assetIds.Select(assetId => new { AssetGroupId = group.Id, AssetId = assetId }).ToList();
        if (rows.Count > 0)
        {
            const string insertMemberSql = """
                INSERT INTO dbo.INV_AssetGroupAssets (AssetGroupId, AssetId) VALUES (@AssetGroupId, @AssetId)
                """;
            await connection.ExecuteAsync(insertMemberSql, rows, transaction);
        }

        transaction.Commit();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM dbo.INV_AssetGroups WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }
}
