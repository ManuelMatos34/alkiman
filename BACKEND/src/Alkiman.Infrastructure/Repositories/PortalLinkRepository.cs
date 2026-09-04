using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.PortalLinks;
using Alkiman.Domain.Entities;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

public class PortalLinkRepository : IPortalLinkRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PortalLinkRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private const string SelectColumns = """
        Id, LandlordId, AssetGroupId, Title, Slug, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
        """;

    public async Task<IReadOnlyList<PortalLink>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.PRT_PortalLinks
            WHERE LandlordId = @LandlordId
            ORDER BY CreatedAt DESC
            """;
        var result = await connection.QueryAsync<PortalLink>(sql, new { LandlordId = landlordId });
        return result.ToList();
    }

    public async Task<PortalLink?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.PRT_PortalLinks
            WHERE Id = @Id
            """;
        return await connection.QuerySingleOrDefaultAsync<PortalLink>(sql, new { Id = id });
    }

    public async Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "SELECT COUNT(1) FROM dbo.PRT_PortalLinks WHERE Slug = @Slug";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Slug = slug });
        return count > 0;
    }

    public async Task<bool> HasPortalRentalsAsync(Guid portalLinkId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "SELECT COUNT(1) FROM dbo.PRT_PortalRentals WHERE PortalLinkId = @PortalLinkId";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { PortalLinkId = portalLinkId });
        return count > 0;
    }

    public async Task<Guid> CreateAsync(PortalLink link, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO dbo.PRT_PortalLinks (Id, LandlordId, AssetGroupId, Title, Slug, IsActive, CreatedAt, CreatedBy)
            VALUES (@Id, @LandlordId, @AssetGroupId, @Title, @Slug, @IsActive, @CreatedAt, @CreatedBy)
            """;
        await connection.ExecuteAsync(sql, link);
        return link.Id;
    }

    public async Task UpdateAsync(PortalLink link, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE dbo.PRT_PortalLinks
            SET Title = @Title,
                AssetGroupId = @AssetGroupId,
                IsActive = @IsActive,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id
            """;
        await connection.ExecuteAsync(sql, link);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM dbo.PRT_PortalLinks WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }
}
