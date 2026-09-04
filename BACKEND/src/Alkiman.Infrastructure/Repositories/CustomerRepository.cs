using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Customers;
using Alkiman.Domain.Entities;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CustomerRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private const string SelectColumns = """
        Id, LandlordId, FullName, IdentityNumber, Phone, Email, Address, Country,
        CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
        """;

    public async Task<IReadOnlyList<Customer>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.CRM_Customers
            WHERE LandlordId = @LandlordId
            ORDER BY FullName
            """;
        var result = await connection.QueryAsync<Customer>(sql, new { LandlordId = landlordId });
        return result.ToList();
    }

    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.CRM_Customers
            WHERE Id = @Id
            """;
        return await connection.QuerySingleOrDefaultAsync<Customer>(sql, new { Id = id });
    }

    public async Task<Guid> CreateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO dbo.CRM_Customers (Id, LandlordId, FullName, IdentityNumber, Phone, Email, Address, Country, CreatedAt, CreatedBy)
            VALUES (@Id, @LandlordId, @FullName, @IdentityNumber, @Phone, @Email, @Address, @Country, @CreatedAt, @CreatedBy)
            """;
        await connection.ExecuteAsync(sql, customer);
        return customer.Id;
    }

    public async Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE dbo.CRM_Customers
            SET FullName = @FullName,
                IdentityNumber = @IdentityNumber,
                Phone = @Phone,
                Email = @Email,
                Address = @Address,
                Country = @Country,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id
            """;
        await connection.ExecuteAsync(sql, customer);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM dbo.CRM_Customers WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }
}
