using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Landlords;
using Alkiman.Domain.Entities;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

public class LandlordRepository : ILandlordRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public LandlordRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Landlord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT Id, BusinessName, AppName, ThemeMode, AccentColor,
                   CountryId, StateId, CityId, Address, Phone1, Phone2, TaxId,
                   SignatureBase64,
                   CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
            FROM dbo.CFG_Landlords
            WHERE Id = @Id
            """;
        return await connection.QuerySingleOrDefaultAsync<Landlord>(sql, new { Id = id });
    }

    public async Task<Guid> CreateAsync(Landlord landlord, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO dbo.CFG_Landlords (Id, BusinessName, CreatedAt, CreatedBy)
            VALUES (@Id, @BusinessName, @CreatedAt, @CreatedBy)
            """;
        await connection.ExecuteAsync(sql, landlord);
        return landlord.Id;
    }

    public async Task UpdateAsync(Landlord landlord, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE dbo.CFG_Landlords
            SET BusinessName = @BusinessName,
                AppName = @AppName,
                ThemeMode = @ThemeMode,
                AccentColor = @AccentColor,
                CountryId = @CountryId,
                StateId = @StateId,
                CityId = @CityId,
                Address = @Address,
                Phone1 = @Phone1,
                Phone2 = @Phone2,
                TaxId = @TaxId,
                SignatureBase64 = @SignatureBase64,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id
            """;
        await connection.ExecuteAsync(sql, landlord);
    }
}
