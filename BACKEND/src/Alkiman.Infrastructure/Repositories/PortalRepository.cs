using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Portal;
using Alkiman.Domain.Entities;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

public class PortalRepository : IPortalRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PortalRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PortalLink?> GetActiveLinkBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT Id, LandlordId, AssetGroupId, Title, Slug, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
            FROM dbo.PRT_PortalLinks
            WHERE Slug = @Slug AND IsActive = 1
            """;
        return await connection.QuerySingleOrDefaultAsync<PortalLink>(sql, new { Slug = slug });
    }

    public async Task<IReadOnlyList<PortalAssetResponse>> GetCatalogAssetsAsync(int assetGroupId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT a.Id, a.Name, a.Description, a.ImageUrl, c.Name AS CategoryName, a.BasePrice, a.RentalType
            FROM dbo.INV_AssetGroupAssets ga
            INNER JOIN dbo.INV_Assets a ON a.Id = ga.AssetId
            INNER JOIN dbo.CFG_Categories c ON c.Id = a.CategoryId
            WHERE ga.AssetGroupId = @AssetGroupId AND a.Status = 'Available'
            ORDER BY a.Name
            """;
        // PortalAssetResponse es un record: Dapper lo materializa haciendo matching por
        // nombre entre las columnas del SELECT y los parámetros del constructor.
        var result = await connection.QueryAsync<PortalAssetResponse>(sql, new { AssetGroupId = assetGroupId });
        return result.ToList();
    }

    public async Task CreatePortalRentalAsync(PortalRental portalRental, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO dbo.PRT_PortalRentals
                (Id, PortalLinkId, RentalId, CustomerId, Quantity, Periods, PaymentProvider, PaymentReference, CreatedAt)
            VALUES
                (@Id, @PortalLinkId, @RentalId, @CustomerId, @Quantity, @Periods, @PaymentProvider, @PaymentReference, @CreatedAt)
            """;
        await connection.ExecuteAsync(sql, portalRental);
    }
}
