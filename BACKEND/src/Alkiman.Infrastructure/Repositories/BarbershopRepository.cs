using Alkiman.Application.Barbershop;
using Alkiman.Application.Common.Interfaces;
using Dapper;
using BarbershopAppointmentEntity = Alkiman.Domain.Entities.BarbershopAppointment;
using BarbershopPortalLinkEntity = Alkiman.Domain.Entities.BarbershopPortalLink;
using BarbershopServiceEntity = Alkiman.Domain.Entities.BarbershopService;
using BarbershopStylistEntity = Alkiman.Domain.Entities.BarbershopStylist;

namespace Alkiman.Infrastructure.Repositories;

public class BarbershopRepository : IBarbershopRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public BarbershopRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    // ============================================================
    // Services
    // ============================================================

    private const string ServiceSelectColumns = """
        Id, LandlordId, Name, Description, Price, DurationMinutes, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
        """;

    public async Task<IReadOnlyList<BarbershopServiceEntity>> GetServicesByLandlordAsync(Guid landlordId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = $"""
            SELECT {ServiceSelectColumns}
            FROM dbo.BRB_Services
            WHERE LandlordId = @LandlordId
            ORDER BY Name
            """;
        var result = await connection.QueryAsync<BarbershopServiceEntity>(sql, new { LandlordId = landlordId });
        return result.ToList();
    }

    public async Task<BarbershopServiceEntity?> GetServiceByIdAsync(int id, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = $"""
            SELECT {ServiceSelectColumns}
            FROM dbo.BRB_Services
            WHERE Id = @Id
            """;
        return await connection.QuerySingleOrDefaultAsync<BarbershopServiceEntity>(sql, new { Id = id });
    }

    public async Task<int> CreateServiceAsync(BarbershopServiceEntity entity, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = """
            INSERT INTO dbo.BRB_Services (LandlordId, Name, Description, Price, DurationMinutes, IsActive, CreatedAt, CreatedBy)
            OUTPUT INSERTED.Id
            VALUES (@LandlordId, @Name, @Description, @Price, @DurationMinutes, @IsActive, @CreatedAt, @CreatedBy)
            """;
        return await connection.ExecuteScalarAsync<int>(sql, entity);
    }

    public async Task UpdateServiceAsync(BarbershopServiceEntity entity, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = """
            UPDATE dbo.BRB_Services
            SET Name           = @Name,
                Description    = @Description,
                Price          = @Price,
                DurationMinutes = @DurationMinutes,
                IsActive       = @IsActive,
                UpdatedAt      = @UpdatedAt,
                UpdatedBy      = @UpdatedBy
            WHERE Id = @Id
            """;
        await connection.ExecuteAsync(sql, entity);
    }

    public async Task DeleteServiceAsync(int id, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = "DELETE FROM dbo.BRB_Services WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<bool> ServiceHasAppointmentsAsync(int id, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = "SELECT COUNT(1) FROM dbo.BRB_Appointments WHERE ServiceId = @Id";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Id = id });
        return count > 0;
    }

    // ============================================================
    // Stylists
    // ============================================================

    private const string StylistSelectColumns = """
        Id, LandlordId, FullName, Phone, Email, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
        """;

    public async Task<IReadOnlyList<BarbershopStylistEntity>> GetStylistsByLandlordAsync(Guid landlordId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = $"""
            SELECT {StylistSelectColumns}
            FROM dbo.BRB_Stylists
            WHERE LandlordId = @LandlordId
            ORDER BY FullName
            """;
        var result = await connection.QueryAsync<BarbershopStylistEntity>(sql, new { LandlordId = landlordId });
        return result.ToList();
    }

    public async Task<BarbershopStylistEntity?> GetStylistByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = $"""
            SELECT {StylistSelectColumns}
            FROM dbo.BRB_Stylists
            WHERE Id = @Id
            """;
        return await connection.QuerySingleOrDefaultAsync<BarbershopStylistEntity>(sql, new { Id = id });
    }

    public async Task<Guid> CreateStylistAsync(BarbershopStylistEntity entity, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = """
            INSERT INTO dbo.BRB_Stylists (Id, LandlordId, FullName, Phone, Email, IsActive, CreatedAt, CreatedBy)
            VALUES (@Id, @LandlordId, @FullName, @Phone, @Email, @IsActive, @CreatedAt, @CreatedBy)
            """;
        await connection.ExecuteAsync(sql, entity);
        return entity.Id;
    }

    public async Task UpdateStylistAsync(BarbershopStylistEntity entity, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = """
            UPDATE dbo.BRB_Stylists
            SET FullName  = @FullName,
                Phone     = @Phone,
                Email     = @Email,
                IsActive  = @IsActive,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id
            """;
        await connection.ExecuteAsync(sql, entity);
    }

    public async Task DeleteStylistAsync(Guid id, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = "DELETE FROM dbo.BRB_Stylists WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<bool> StylistHasAppointmentsAsync(Guid id, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = "SELECT COUNT(1) FROM dbo.BRB_Appointments WHERE StylistId = @Id";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Id = id });
        return count > 0;
    }

    // ============================================================
    // Portal Links
    // ============================================================

    private const string PortalLinkSelectColumns = """
        Id, LandlordId, StylistId, Title, Slug, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
        """;

    public async Task<IReadOnlyList<BarbershopPortalLinkEntity>> GetPortalLinksByLandlordAsync(Guid landlordId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = $"""
            SELECT {PortalLinkSelectColumns}
            FROM dbo.BRB_PortalLinks
            WHERE LandlordId = @LandlordId
            ORDER BY CreatedAt DESC
            """;
        var result = await connection.QueryAsync<BarbershopPortalLinkEntity>(sql, new { LandlordId = landlordId });
        return result.ToList();
    }

    public async Task<BarbershopPortalLinkEntity?> GetPortalLinkByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = $"""
            SELECT {PortalLinkSelectColumns}
            FROM dbo.BRB_PortalLinks
            WHERE Id = @Id
            """;
        return await connection.QuerySingleOrDefaultAsync<BarbershopPortalLinkEntity>(sql, new { Id = id });
    }

    public async Task<BarbershopPortalLinkEntity?> GetPortalLinkBySlugAsync(string slug, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = $"""
            SELECT {PortalLinkSelectColumns}
            FROM dbo.BRB_PortalLinks
            WHERE Slug = @Slug
            """;
        return await connection.QuerySingleOrDefaultAsync<BarbershopPortalLinkEntity>(sql, new { Slug = slug });
    }

    public async Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = "SELECT COUNT(1) FROM dbo.BRB_PortalLinks WHERE Slug = @Slug";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Slug = slug });
        return count > 0;
    }

    public async Task<Guid> CreatePortalLinkAsync(BarbershopPortalLinkEntity entity, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = """
            INSERT INTO dbo.BRB_PortalLinks (Id, LandlordId, StylistId, Title, Slug, IsActive, CreatedAt, CreatedBy)
            VALUES (@Id, @LandlordId, @StylistId, @Title, @Slug, @IsActive, @CreatedAt, @CreatedBy)
            """;
        await connection.ExecuteAsync(sql, entity);
        return entity.Id;
    }

    public async Task UpdatePortalLinkAsync(BarbershopPortalLinkEntity entity, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = """
            UPDATE dbo.BRB_PortalLinks
            SET StylistId = @StylistId,
                Title     = @Title,
                IsActive  = @IsActive,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id
            """;
        await connection.ExecuteAsync(sql, entity);
    }

    public async Task DeletePortalLinkAsync(Guid id, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = "DELETE FROM dbo.BRB_PortalLinks WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    // ============================================================
    // Appointments
    // ============================================================

    private const string AppointmentSelectColumns = """
        Id, LandlordId, StylistId, ServiceId, PortalLinkId, TrackingToken,
        ClientName, ClientPhone, ClientEmail, Notes, ScheduledAt, Source, Status, IsPaid,
        CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
        """;

    public async Task<IReadOnlyList<BarbershopAppointmentEntity>> GetAppointmentsByLandlordAsync(
        Guid landlordId, DateTime? date, DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        string sql;
        object parameters;

        if (from.HasValue && to.HasValue)
        {
            sql = $"""
                SELECT {AppointmentSelectColumns}
                FROM dbo.BRB_Appointments
                WHERE LandlordId = @LandlordId
                  AND ScheduledAt >= @From
                  AND ScheduledAt < @To
                ORDER BY ScheduledAt
                """;
            parameters = new { LandlordId = landlordId, From = from.Value.Date, To = to.Value.Date.AddDays(1) };
        }
        else if (date.HasValue)
        {
            var day = date.Value.Date;
            sql = $"""
                SELECT {AppointmentSelectColumns}
                FROM dbo.BRB_Appointments
                WHERE LandlordId = @LandlordId
                  AND CAST(ScheduledAt AS DATE) = @Day
                ORDER BY ScheduledAt
                """;
            parameters = new { LandlordId = landlordId, Day = day };
        }
        else
        {
            sql = $"""
                SELECT {AppointmentSelectColumns}
                FROM dbo.BRB_Appointments
                WHERE LandlordId = @LandlordId
                  AND Status NOT IN ('Completed', 'Cancelled')
                ORDER BY ScheduledAt
                """;
            parameters = new { LandlordId = landlordId };
        }

        var result = await connection.QueryAsync<BarbershopAppointmentEntity>(sql, parameters);
        return result.ToList();
    }

    public async Task<BarbershopAppointmentEntity?> GetAppointmentByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = $"""
            SELECT {AppointmentSelectColumns}
            FROM dbo.BRB_Appointments
            WHERE Id = @Id
            """;
        return await connection.QuerySingleOrDefaultAsync<BarbershopAppointmentEntity>(sql, new { Id = id });
    }

    public async Task<BarbershopAppointmentEntity?> GetAppointmentByTokenAsync(string token, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = $"""
            SELECT {AppointmentSelectColumns}
            FROM dbo.BRB_Appointments
            WHERE TrackingToken = @Token
            """;
        return await connection.QuerySingleOrDefaultAsync<BarbershopAppointmentEntity>(sql, new { Token = token });
    }

    public async Task<Guid> CreateAppointmentAsync(BarbershopAppointmentEntity entity, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = """
            INSERT INTO dbo.BRB_Appointments
                (Id, LandlordId, StylistId, ServiceId, PortalLinkId, TrackingToken,
                 ClientName, ClientPhone, ClientEmail, Notes, ScheduledAt, Source, Status, IsPaid,
                 CreatedAt, CreatedBy)
            VALUES
                (@Id, @LandlordId, @StylistId, @ServiceId, @PortalLinkId, @TrackingToken,
                 @ClientName, @ClientPhone, @ClientEmail, @Notes, @ScheduledAt, @Source, @Status, @IsPaid,
                 @CreatedAt, @CreatedBy)
            """;
        await connection.ExecuteAsync(sql, entity);
        return entity.Id;
    }

    public async Task UpdateAppointmentAsync(BarbershopAppointmentEntity entity, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = """
            UPDATE dbo.BRB_Appointments
            SET StylistId   = @StylistId,
                ServiceId   = @ServiceId,
                ClientName  = @ClientName,
                ClientPhone = @ClientPhone,
                ClientEmail = @ClientEmail,
                Notes       = @Notes,
                ScheduledAt = @ScheduledAt,
                Status      = @Status,
                IsPaid      = @IsPaid,
                UpdatedAt   = @UpdatedAt,
                UpdatedBy   = @UpdatedBy
            WHERE Id = @Id
            """;
        await connection.ExecuteAsync(sql, entity);
    }

    // ============================================================
    // Metrics
    // ============================================================

    public async Task<IReadOnlyList<BarbershopAppointmentEntity>> GetAppointmentsForMetricsAsync(
        Guid landlordId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = $"""
            SELECT {AppointmentSelectColumns}
            FROM dbo.BRB_Appointments
            WHERE LandlordId = @LandlordId
              AND ScheduledAt >= @From
              AND ScheduledAt < @To
            ORDER BY ScheduledAt
            """;
        var result = await connection.QueryAsync<BarbershopAppointmentEntity>(sql, new { LandlordId = landlordId, From = from, To = to });
        return result.ToList();
    }

    // ============================================================
    // Lookup helpers (used by service layer for cross-entity validation)
    // ============================================================

    public async Task<BarbershopStylistEntity?> GetStylistByAppointmentLandlordAsync(Guid stylistId, Guid landlordId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = $"""
            SELECT {StylistSelectColumns}
            FROM dbo.BRB_Stylists
            WHERE Id = @StylistId AND LandlordId = @LandlordId
            """;
        return await connection.QuerySingleOrDefaultAsync<BarbershopStylistEntity>(sql, new { StylistId = stylistId, LandlordId = landlordId });
    }

    public async Task<BarbershopServiceEntity?> GetServiceByAppointmentLandlordAsync(int serviceId, Guid landlordId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = $"""
            SELECT {ServiceSelectColumns}
            FROM dbo.BRB_Services
            WHERE Id = @ServiceId AND LandlordId = @LandlordId
            """;
        return await connection.QuerySingleOrDefaultAsync<BarbershopServiceEntity>(sql, new { ServiceId = serviceId, LandlordId = landlordId });
    }
}
