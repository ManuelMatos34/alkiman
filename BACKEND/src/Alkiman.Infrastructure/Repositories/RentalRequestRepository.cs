using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.RentalRequests;
using Alkiman.Domain.Entities;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

public class RentalRequestRepository : IRentalRequestRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RentalRequestRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private const string SelectColumns = """
        Id, LandlordId, RentalId, Type, Status, RequestedPeriods, ProposedEndDate, Reason,
        StaffNote, ReviewedAt, ReviewedBy, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
        """;

    public async Task<IReadOnlyList<RentalRequest>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.TRX_RentalRequests
            WHERE LandlordId = @LandlordId
            ORDER BY CreatedAt DESC
            """;
        var result = await connection.QueryAsync<RentalRequest>(sql, new { LandlordId = landlordId });
        return result.ToList();
    }

    public async Task<RentalRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.TRX_RentalRequests
            WHERE Id = @Id
            """;
        return await connection.QuerySingleOrDefaultAsync<RentalRequest>(sql, new { Id = id });
    }

    public async Task<RentalRequest?> GetPendingByRentalAsync(Guid rentalId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.TRX_RentalRequests
            WHERE RentalId = @RentalId AND Status = 'Pending'
            """;
        return await connection.QuerySingleOrDefaultAsync<RentalRequest>(sql, new { RentalId = rentalId });
    }

    public async Task<Guid> CreateAsync(RentalRequest request, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO dbo.TRX_RentalRequests
                (Id, LandlordId, RentalId, Type, Status, RequestedPeriods, ProposedEndDate, Reason,
                 StaffNote, ReviewedAt, ReviewedBy, CreatedAt, CreatedBy)
            VALUES
                (@Id, @LandlordId, @RentalId, @Type, @Status, @RequestedPeriods, @ProposedEndDate, @Reason,
                 @StaffNote, @ReviewedAt, @ReviewedBy, @CreatedAt, @CreatedBy)
            """;
        // Nota (ver RentalRepository.CreateAsync): Dapper convierte los enums a su tipo
        // subyacente (int) en LookupDbType *antes* de consultar los TypeHandler registrados,
        // por lo que un TypeHandler<T> para un enum nunca se aplica cuando se pasa la entidad
        // completa como parámetros. Convertimos a texto explícitamente para evitar violar
        // los CHECK constraints.
        await connection.ExecuteAsync(sql, new
        {
            request.Id,
            request.LandlordId,
            request.RentalId,
            Type = request.Type.ToString(),
            Status = request.Status.ToString(),
            request.RequestedPeriods,
            request.ProposedEndDate,
            request.Reason,
            request.StaffNote,
            request.ReviewedAt,
            request.ReviewedBy,
            request.CreatedAt,
            request.CreatedBy
        });
        return request.Id;
    }

    public async Task UpdateAsync(RentalRequest request, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE dbo.TRX_RentalRequests
            SET Status = @Status,
                StaffNote = @StaffNote,
                ReviewedAt = @ReviewedAt,
                ReviewedBy = @ReviewedBy,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id
            """;
        // Ver nota en CreateAsync: convertimos el enum a texto explícitamente.
        await connection.ExecuteAsync(sql, new
        {
            request.Id,
            Status = request.Status.ToString(),
            request.StaffNote,
            request.ReviewedAt,
            request.ReviewedBy,
            request.UpdatedAt,
            request.UpdatedBy
        });
    }
}
