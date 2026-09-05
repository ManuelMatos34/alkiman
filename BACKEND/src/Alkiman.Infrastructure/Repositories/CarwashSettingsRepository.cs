using Alkiman.Application.Carwash;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Domain.Entities;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

public class CarwashSettingsRepository : ICarwashSettingsRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CarwashSettingsRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<CarwashSettings?> GetByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT LandlordId, OperationMode, TipMode, TipSuggestedPercent, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
            FROM dbo.CWS_Settings
            WHERE LandlordId = @LandlordId
            """;
        return await connection.QuerySingleOrDefaultAsync<CarwashSettings>(sql, new { LandlordId = landlordId });
    }

    public async Task UpsertAsync(CarwashSettings settings, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE dbo.CWS_Settings
            SET OperationMode = @OperationMode,
                TipMode = @TipMode,
                TipSuggestedPercent = @TipSuggestedPercent,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE LandlordId = @LandlordId;

            IF @@ROWCOUNT = 0
                INSERT INTO dbo.CWS_Settings (LandlordId, OperationMode, TipMode, TipSuggestedPercent, CreatedAt, CreatedBy)
                VALUES (@LandlordId, @OperationMode, @TipMode, @TipSuggestedPercent, @CreatedAt, @CreatedBy);
            """;
        await connection.ExecuteAsync(sql, settings);
    }
}
