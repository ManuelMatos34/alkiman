using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Payments;
using Alkiman.Domain.Entities;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PaymentRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private const string SelectColumns = """
        Id, RentalId, LandlordId, Amount, Type, PaymentDate, StripeTransactionId,
        CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
        """;

    public async Task<IReadOnlyList<Payment>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.TRX_Payments
            WHERE LandlordId = @LandlordId
            ORDER BY PaymentDate DESC
            """;
        var result = await connection.QueryAsync<Payment>(sql, new { LandlordId = landlordId });
        return result.ToList();
    }

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.TRX_Payments
            WHERE Id = @Id
            """;
        return await connection.QuerySingleOrDefaultAsync<Payment>(sql, new { Id = id });
    }

    public async Task<Guid> CreateAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO dbo.TRX_Payments
                (Id, RentalId, LandlordId, Amount, Type, PaymentDate, StripeTransactionId, CreatedAt, CreatedBy)
            VALUES
                (@Id, @RentalId, @LandlordId, @Amount, @Type, @PaymentDate, @StripeTransactionId, @CreatedAt, @CreatedBy)
            """;
        await connection.ExecuteAsync(sql, payment);
        return payment.Id;
    }
}
