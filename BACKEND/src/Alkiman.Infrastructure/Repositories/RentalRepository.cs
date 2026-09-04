using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Rentals;
using Alkiman.Domain.Entities;
using Alkiman.Domain.Enums;
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
        r.Status, r.AccessToken, r.AccessFailedAttempts, r.AccessLockedUntil,
        r.CreatedAt, r.CreatedBy, r.UpdatedAt, r.UpdatedBy
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

    public async Task<Rental?> GetByAccessTokenAsync(Guid accessToken, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.TRX_Rentals r
            WHERE r.AccessToken = @AccessToken
            """;
        return await connection.QuerySingleOrDefaultAsync<Rental>(sql, new { AccessToken = accessToken });
    }

    public async Task<Guid> CreateAsync(Rental rental, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO dbo.TRX_Rentals
                (Id, AssetId, CustomerId, StartDate, EndDate, ContractPdfUrl, TotalPrice, Status, AccessToken, CreatedAt, CreatedBy)
            VALUES
                (@Id, @AssetId, @CustomerId, @StartDate, @EndDate, @ContractPdfUrl, @TotalPrice, @Status, @AccessToken, @CreatedAt, @CreatedBy)
            """;
        // Nota: Dapper convierte los enums a su tipo subyacente (int) en LookupDbType
        // *antes* de consultar los TypeHandler registrados, por lo que un TypeHandler<T>
        // para un enum nunca se aplica cuando se pasa la entidad completa como parámetros.
        // Convertimos a texto explícitamente para evitar violar los CHECK constraints.
        await connection.ExecuteAsync(sql, new
        {
            rental.Id,
            rental.AssetId,
            rental.CustomerId,
            rental.StartDate,
            rental.EndDate,
            rental.ContractPdfUrl,
            rental.TotalPrice,
            Status = rental.Status.ToString(),
            rental.AccessToken,
            rental.CreatedAt,
            rental.CreatedBy
        });
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
        // Ver nota en CreateAsync: convertimos el enum a texto explícitamente.
        await connection.ExecuteAsync(sql, new
        {
            rental.Id,
            rental.StartDate,
            rental.EndDate,
            rental.ContractPdfUrl,
            rental.TotalPrice,
            Status = rental.Status.ToString(),
            rental.UpdatedAt,
            rental.UpdatedBy
        });
    }

    public async Task UpdateAccessStateAsync(Guid rentalId, int accessFailedAttempts, DateTime? accessLockedUntil, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE dbo.TRX_Rentals
            SET AccessFailedAttempts = @AccessFailedAttempts,
                AccessLockedUntil = @AccessLockedUntil
            WHERE Id = @RentalId
            """;
        await connection.ExecuteAsync(sql, new { RentalId = rentalId, AccessFailedAttempts = accessFailedAttempts, AccessLockedUntil = accessLockedUntil });
    }

    public async Task<IReadOnlyList<RentalDueSummary>> GetActiveRentalsDueOnAsync(DateTime dueDate, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT r.Id AS RentalId, a.LandlordId, r.CustomerId, a.Name AS AssetName, r.EndDate, r.TotalPrice
            FROM dbo.TRX_Rentals r
            INNER JOIN dbo.INV_Assets a ON a.Id = r.AssetId
            WHERE r.Status = @Status AND CAST(r.EndDate AS DATE) = CAST(@DueDate AS DATE)
            """;
        // Ver nota en CreateAsync/UpdateAsync: comparamos contra el texto del enum, no un TypeHandler.
        var result = await connection.QueryAsync<RentalDueSummary>(sql, new { Status = nameof(RentalStatus.Active), DueDate = dueDate.Date });
        return result.ToList();
    }
}
