using Alkiman.Application.Carwash;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Domain.Entities;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

public class CarwashPortalLinkRepository : ICarwashPortalLinkRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CarwashPortalLinkRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private const string SelectColumns = """
        Id, LandlordId, Title, Slug, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
        """;

    public async Task<IReadOnlyList<CarwashPortalLink>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.CWS_PortalLinks
            WHERE LandlordId = @LandlordId
            ORDER BY CreatedAt DESC
            """;
        var result = await connection.QueryAsync<CarwashPortalLink>(sql, new { LandlordId = landlordId });
        return result.ToList();
    }

    public async Task<CarwashPortalLink?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.CWS_PortalLinks
            WHERE Id = @Id
            """;
        return await connection.QuerySingleOrDefaultAsync<CarwashPortalLink>(sql, new { Id = id });
    }

    public async Task<CarwashPortalLink?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.CWS_PortalLinks
            WHERE Slug = @Slug
            """;
        return await connection.QuerySingleOrDefaultAsync<CarwashPortalLink>(sql, new { Slug = slug });
    }

    public async Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "SELECT COUNT(1) FROM dbo.CWS_PortalLinks WHERE Slug = @Slug";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Slug = slug });
        return count > 0;
    }

    public async Task<Guid> CreateAsync(CarwashPortalLink link, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO dbo.CWS_PortalLinks (Id, LandlordId, Title, Slug, IsActive, CreatedAt, CreatedBy)
            VALUES (@Id, @LandlordId, @Title, @Slug, @IsActive, @CreatedAt, @CreatedBy)
            """;
        await connection.ExecuteAsync(sql, link);
        return link.Id;
    }

    public async Task UpdateAsync(CarwashPortalLink link, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE dbo.CWS_PortalLinks
            SET Title = @Title,
                IsActive = @IsActive,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id
            """;
        await connection.ExecuteAsync(sql, link);
    }
}
