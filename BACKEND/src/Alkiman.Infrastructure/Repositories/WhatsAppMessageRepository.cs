using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.WhatsApp;
using Alkiman.Domain.Entities;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

public class WhatsAppMessageRepository : IWhatsAppMessageRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public WhatsAppMessageRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task CreateAsync(WhatsAppMessage message, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = """
            INSERT INTO dbo.COM_WhatsAppMessages
                (LandlordId, ToPhone, TemplateName, Status, ErrorMessage, SentAt, CreatedAt)
            VALUES
                (@LandlordId, @ToPhone, @TemplateName, @Status, @ErrorMessage, @SentAt, @CreatedAt)
            """;
        await connection.ExecuteAsync(sql, message);
    }

    public async Task<int> GetMonthlyCountAsync(Guid landlordId, int year, int month, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = """
            SELECT COUNT(*)
            FROM dbo.COM_WhatsAppMessages
            WHERE LandlordId = @LandlordId
              AND Status = 'Sent'
              AND YEAR(CreatedAt) = @Year
              AND MONTH(CreatedAt) = @Month
            """;
        return await connection.ExecuteScalarAsync<int>(sql, new { LandlordId = landlordId, Year = year, Month = month });
    }
}
