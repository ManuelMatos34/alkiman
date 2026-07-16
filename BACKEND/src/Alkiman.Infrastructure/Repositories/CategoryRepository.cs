using Alkiman.Application.Categories;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Domain.Entities;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CategoryRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Category>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT Id, LandlordId, Name, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
            FROM dbo.CFG_Categories
            WHERE LandlordId = @LandlordId
            ORDER BY Name
            """;
        var result = await connection.QueryAsync<Category>(sql, new { LandlordId = landlordId });
        return result.ToList();
    }

    public async Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT Id, LandlordId, Name, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
            FROM dbo.CFG_Categories
            WHERE Id = @Id
            """;
        return await connection.QuerySingleOrDefaultAsync<Category>(sql, new { Id = id });
    }

    public async Task<int> CreateAsync(Category category, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO dbo.CFG_Categories (LandlordId, Name, CreatedAt, CreatedBy)
            OUTPUT INSERTED.Id
            VALUES (@LandlordId, @Name, @CreatedAt, @CreatedBy)
            """;
        return await connection.ExecuteScalarAsync<int>(sql, category);
    }

    public async Task UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE dbo.CFG_Categories
            SET Name = @Name,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id
            """;
        await connection.ExecuteAsync(sql, category);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM dbo.CFG_Categories WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }
}
