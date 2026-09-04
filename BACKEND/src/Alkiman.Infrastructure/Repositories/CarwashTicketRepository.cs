using Alkiman.Application.Carwash;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Domain.Entities;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

public class CarwashTicketRepository : ICarwashTicketRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CarwashTicketRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private const string SelectColumns = """
        Id, LandlordId, CustomerId, ServiceId, AssignedToWasherId, QueueNumber, VehiclePlate,
        VehicleBrand, VehicleModel, VehicleYear, VehicleColor, ServicePrice,
        Status, Source, AccessToken, ArrivalDeadline, ArrivedAt, StartedAt, ReadyAt, DeliveredAt, CancelledAt, Notes,
        CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
        """;

    public async Task<IReadOnlyList<CarwashTicket>> GetActiveByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.CWS_Tickets
            WHERE LandlordId = @LandlordId
              AND Status NOT IN ('Delivered', 'Cancelled', 'Expired')
            ORDER BY QueueNumber
            """;
        var result = await connection.QueryAsync<CarwashTicket>(sql, new { LandlordId = landlordId });
        return result.ToList();
    }

    public async Task<CarwashTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.CWS_Tickets
            WHERE Id = @Id
            """;
        return await connection.QuerySingleOrDefaultAsync<CarwashTicket>(sql, new { Id = id });
    }

    public async Task<CarwashTicket?> GetByAccessTokenAsync(Guid accessToken, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.CWS_Tickets
            WHERE AccessToken = @AccessToken
            """;
        return await connection.QuerySingleOrDefaultAsync<CarwashTicket>(sql, new { AccessToken = accessToken });
    }

    public async Task<Guid> CreateAsync(CarwashTicket ticket, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO dbo.CWS_Tickets
                (Id, LandlordId, CustomerId, ServiceId, AssignedToWasherId, QueueNumber, VehiclePlate,
                 VehicleBrand, VehicleModel, VehicleYear, VehicleColor, ServicePrice,
                 Status, Source, AccessToken, ArrivalDeadline, ArrivedAt, StartedAt, ReadyAt, DeliveredAt, CancelledAt, Notes,
                 CreatedAt, CreatedBy)
            VALUES
                (@Id, @LandlordId, @CustomerId, @ServiceId, @AssignedToWasherId, @QueueNumber, @VehiclePlate,
                 @VehicleBrand, @VehicleModel, @VehicleYear, @VehicleColor, @ServicePrice,
                 @Status, @Source, @AccessToken, @ArrivalDeadline, @ArrivedAt, @StartedAt, @ReadyAt, @DeliveredAt, @CancelledAt, @Notes,
                 @CreatedAt, @CreatedBy)
            """;
        await connection.ExecuteAsync(sql, ticket);
        return ticket.Id;
    }

    public async Task UpdateAsync(CarwashTicket ticket, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE dbo.CWS_Tickets
            SET CustomerId = @CustomerId,
                ServiceId = @ServiceId,
                AssignedToWasherId = @AssignedToWasherId,
                VehiclePlate = @VehiclePlate,
                VehicleBrand = @VehicleBrand,
                VehicleModel = @VehicleModel,
                VehicleYear = @VehicleYear,
                VehicleColor = @VehicleColor,
                ServicePrice = @ServicePrice,
                Status = @Status,
                ArrivalDeadline = @ArrivalDeadline,
                ArrivedAt = @ArrivedAt,
                StartedAt = @StartedAt,
                ReadyAt = @ReadyAt,
                DeliveredAt = @DeliveredAt,
                CancelledAt = @CancelledAt,
                Notes = @Notes,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id
            """;
        await connection.ExecuteAsync(sql, ticket);
    }

    public async Task<int> GetNextQueueNumberAsync(Guid landlordId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT ISNULL(MAX(QueueNumber), 0) + 1
            FROM dbo.CWS_Tickets
            WHERE LandlordId = @LandlordId
              AND CAST(CreatedAt AS DATE) = CAST(SYSUTCDATETIME() AS DATE)
            """;
        return await connection.ExecuteScalarAsync<int>(sql, new { LandlordId = landlordId });
    }

    public async Task AddExtrasAsync(IEnumerable<CarwashTicketExtra> extras, CancellationToken cancellationToken = default)
    {
        var rows = extras.ToList();
        if (rows.Count == 0)
            return;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO dbo.CWS_TicketExtras (TicketId, ExtraId, Name, Price)
            VALUES (@TicketId, @ExtraId, @Name, @Price)
            """;
        // Dapper expande la lista en un INSERT por fila dentro del mismo comando.
        await connection.ExecuteAsync(sql, rows);
    }

    public async Task<IReadOnlyList<CarwashTicketExtra>> GetExtrasByTicketIdsAsync(IReadOnlyCollection<Guid> ticketIds, CancellationToken cancellationToken = default)
    {
        if (ticketIds.Count == 0)
            return [];

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT TicketId, ExtraId, Name, Price
            FROM dbo.CWS_TicketExtras
            WHERE TicketId IN @TicketIds
            ORDER BY Name
            """;
        var result = await connection.QueryAsync<CarwashTicketExtra>(sql, new { TicketIds = ticketIds });
        return result.ToList();
    }

    public async Task<bool> HasTicketsWithExtraAsync(int extraId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.CWS_TicketExtras WHERE ExtraId = @ExtraId) THEN 1 ELSE 0 END";
        return await connection.ExecuteScalarAsync<bool>(sql, new { ExtraId = extraId });
    }
}
