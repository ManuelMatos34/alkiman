using Alkiman.Application.ContractTemplates;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Domain.Entities;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

public class ContractTemplateRepository : IContractTemplateRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ContractTemplateRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private const string SelectColumns = """
        Id, LandlordId, CategoryId, Name, Content, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
        """;

    public async Task<IReadOnlyList<ContractTemplate>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.COM_ContractTemplates
            WHERE LandlordId = @LandlordId
            ORDER BY CategoryId, IsActive DESC, CreatedAt DESC
            """;
        var result = await connection.QueryAsync<ContractTemplate>(sql, new { LandlordId = landlordId });
        return result.ToList();
    }

    public async Task<ContractTemplate?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.COM_ContractTemplates
            WHERE Id = @Id
            """;
        return await connection.QuerySingleOrDefaultAsync<ContractTemplate>(sql, new { Id = id });
    }

    public async Task<ContractTemplate?> GetActiveByCategoryAsync(Guid landlordId, int categoryId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT {SelectColumns}
            FROM dbo.COM_ContractTemplates
            WHERE LandlordId = @LandlordId AND CategoryId = @CategoryId AND IsActive = 1
            """;
        return await connection.QuerySingleOrDefaultAsync<ContractTemplate>(sql, new { LandlordId = landlordId, CategoryId = categoryId });
    }

    public async Task<int> CreateAsync(ContractTemplate template, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO dbo.COM_ContractTemplates
                (LandlordId, CategoryId, Name, Content, IsActive, CreatedAt, CreatedBy)
            OUTPUT INSERTED.Id
            VALUES
                (@LandlordId, @CategoryId, @Name, @Content, @IsActive, @CreatedAt, @CreatedBy)
            """;
        return await connection.ExecuteScalarAsync<int>(sql, template);
    }

    public async Task UpdateAsync(ContractTemplate template, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE dbo.COM_ContractTemplates
            SET Name = @Name,
                Content = @Content,
                IsActive = @IsActive,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id
            """;
        await connection.ExecuteAsync(sql, template);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM dbo.COM_ContractTemplates WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task DeactivateAllForCategoryAsync(Guid landlordId, int categoryId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE dbo.COM_ContractTemplates
            SET IsActive = 0
            WHERE LandlordId = @LandlordId AND CategoryId = @CategoryId AND IsActive = 1
            """;
        await connection.ExecuteAsync(sql, new { LandlordId = landlordId, CategoryId = categoryId });
    }
}
