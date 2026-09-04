using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Reminders;
using Alkiman.Domain.Entities;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

public class ReminderRepository : IReminderRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ReminderRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<ReminderRaw>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT r.Id, r.Title, r.Message, r.RemindAt, r.CustomerId, c.FullName AS CustomerName,
                   r.RentalId, r.Status, r.CreatedAt
            FROM dbo.COM_Reminders r
            LEFT JOIN dbo.CRM_Customers c ON c.Id = r.CustomerId
            WHERE r.LandlordId = @LandlordId
            ORDER BY r.RemindAt ASC
            """;
        var result = await connection.QueryAsync<ReminderRaw>(sql, new { LandlordId = landlordId });
        return result.ToList();
    }

    public async Task<Reminder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT Id, LandlordId, CustomerId, RentalId, Title, Message, RemindAt, Status,
                   CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
            FROM dbo.COM_Reminders
            WHERE Id = @Id
            """;
        return await connection.QuerySingleOrDefaultAsync<Reminder>(sql, new { Id = id });
    }

    public async Task<Guid> CreateAsync(Reminder reminder, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO dbo.COM_Reminders
                (LandlordId, CustomerId, RentalId, Title, Message, RemindAt, Status, CreatedAt, CreatedBy)
            OUTPUT INSERTED.Id
            VALUES
                (@LandlordId, @CustomerId, @RentalId, @Title, @Message, @RemindAt, @Status, @CreatedAt, @CreatedBy)
            """;
        return await connection.ExecuteScalarAsync<Guid>(sql, reminder);
    }

    public async Task UpdateAsync(Reminder reminder, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE dbo.COM_Reminders
            SET Title = @Title,
                Message = @Message,
                RemindAt = @RemindAt,
                CustomerId = @CustomerId,
                RentalId = @RentalId,
                Status = @Status,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id
            """;
        await connection.ExecuteAsync(sql, reminder);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM dbo.COM_Reminders WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<bool> ExistsAutoReminderForRentalAsync(Guid rentalId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT COUNT(1) FROM dbo.COM_Reminders
            WHERE RentalId = @RentalId AND CreatedBy = @CreatedBy
            """;
        var count = await connection.ExecuteScalarAsync<int>(sql, new { RentalId = rentalId, CreatedBy = AutoReminderDefaults.RentalDueCreatedBy });
        return count > 0;
    }
}
