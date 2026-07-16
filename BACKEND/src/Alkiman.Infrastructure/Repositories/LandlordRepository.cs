using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Landlords;
using Alkiman.Domain.Entities;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

public class LandlordRepository : ILandlordRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public LandlordRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Landlord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT Id, Auth0UserId, BusinessName, Email, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
            FROM dbo.CFG_Landlords
            WHERE Id = @Id
            """;
        return await connection.QuerySingleOrDefaultAsync<Landlord>(sql, new { Id = id });
    }

    public async Task<Landlord?> GetByAuth0UserIdAsync(string auth0UserId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT Id, Auth0UserId, BusinessName, Email, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
            FROM dbo.CFG_Landlords
            WHERE Auth0UserId = @Auth0UserId
            """;
        return await connection.QuerySingleOrDefaultAsync<Landlord>(sql, new { Auth0UserId = auth0UserId });
    }

    public async Task<Guid> CreateAsync(Landlord landlord, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO dbo.CFG_Landlords (Id, Auth0UserId, BusinessName, Email, CreatedAt, CreatedBy)
            VALUES (@Id, @Auth0UserId, @BusinessName, @Email, @CreatedAt, @CreatedBy)
            """;
        await connection.ExecuteAsync(sql, landlord);
        return landlord.Id;
    }

    public async Task UpdateAsync(Landlord landlord, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE dbo.CFG_Landlords
            SET BusinessName = @BusinessName,
                Email = @Email,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id
            """;
        await connection.ExecuteAsync(sql, landlord);
    }
}
