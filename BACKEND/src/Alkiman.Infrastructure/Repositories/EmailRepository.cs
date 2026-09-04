using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Emails;
using Alkiman.Domain.Entities;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

public class EmailRepository : IEmailRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public EmailRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<EmailMessage>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT Id, LandlordId, CustomerId, Type, RecipientName, RecipientEmail, Subject, Body,
                   Status, ErrorMessage, SentAt, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
            FROM dbo.COM_EmailMessages
            WHERE LandlordId = @LandlordId
            ORDER BY CreatedAt DESC
            """;
        var result = await connection.QueryAsync<EmailMessage>(sql, new { LandlordId = landlordId });
        return result.ToList();
    }

    public async Task<Guid> CreateAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO dbo.COM_EmailMessages
                (LandlordId, CustomerId, Type, RecipientName, RecipientEmail, Subject, Body,
                 Status, ErrorMessage, SentAt, CreatedAt, CreatedBy)
            OUTPUT INSERTED.Id
            VALUES
                (@LandlordId, @CustomerId, @Type, @RecipientName, @RecipientEmail, @Subject, @Body,
                 @Status, @ErrorMessage, @SentAt, @CreatedAt, @CreatedBy)
            """;
        return await connection.ExecuteScalarAsync<Guid>(sql, message);
    }
}
