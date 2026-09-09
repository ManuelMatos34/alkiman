using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.WhatsApp;
using Alkiman.Domain.Entities;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

public class LandlordWhatsAppRepository : ILandlordWhatsAppRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public LandlordWhatsAppRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<LandlordWhatsApp?> GetByLandlordIdAsync(Guid landlordId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = "SELECT * FROM dbo.CFG_LandlordWhatsApp WHERE LandlordId = @LandlordId";
        return await connection.QuerySingleOrDefaultAsync<LandlordWhatsApp>(sql, new { LandlordId = landlordId });
    }

    public async Task UpsertAsync(LandlordWhatsApp config, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = """
            MERGE dbo.CFG_LandlordWhatsApp AS target
            USING (SELECT @LandlordId AS LandlordId) AS source
            ON target.LandlordId = source.LandlordId
            WHEN MATCHED THEN
                UPDATE SET
                    PhoneNumberId = @PhoneNumberId,
                    AccessToken   = @AccessToken,
                    PhoneNumber   = @PhoneNumber,
                    DisplayName   = @DisplayName,
                    IsActive      = @IsActive,
                    UpdatedAt     = @UpdatedAt,
                    UpdatedBy     = @UpdatedBy
            WHEN NOT MATCHED THEN
                INSERT (Id, LandlordId, PhoneNumberId, AccessToken, PhoneNumber, DisplayName, IsActive, CreatedAt, CreatedBy)
                VALUES (@Id, @LandlordId, @PhoneNumberId, @AccessToken, @PhoneNumber, @DisplayName, @IsActive, @CreatedAt, @CreatedBy);
            """;
        await connection.ExecuteAsync(sql, config);
    }
}
