using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Users;
using Alkiman.Domain.Entities;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<User>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT Id, LandlordId, RoleId, FullName, Email, PasswordHash, IsOwner, IsActive, TwoFactorEnabled,
                   MustChangePassword, ResetToken, ResetTokenExpiresAt,
                   CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
            FROM dbo.CFG_Users
            WHERE LandlordId = @LandlordId
            ORDER BY FullName
            """;
        var result = await connection.QueryAsync<User>(sql, new { LandlordId = landlordId });
        return result.ToList();
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT Id, LandlordId, RoleId, FullName, Email, PasswordHash, IsOwner, IsActive, TwoFactorEnabled,
                   MustChangePassword, ResetToken, ResetTokenExpiresAt,
                   CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
            FROM dbo.CFG_Users
            WHERE Id = @Id
            """;
        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Id = id });
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT Id, LandlordId, RoleId, FullName, Email, PasswordHash, IsOwner, IsActive, TwoFactorEnabled,
                   MustChangePassword, ResetToken, ResetTokenExpiresAt,
                   CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
            FROM dbo.CFG_Users
            WHERE Email = @Email
            """;
        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Email = email });
    }

    public async Task<Guid> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            INSERT INTO dbo.CFG_Users
                (Id, LandlordId, RoleId, FullName, Email, PasswordHash, IsOwner, IsActive, TwoFactorEnabled,
                 MustChangePassword, CreatedAt, CreatedBy)
            VALUES
                (@Id, @LandlordId, @RoleId, @FullName, @Email, @PasswordHash, @IsOwner, @IsActive, @TwoFactorEnabled,
                 @MustChangePassword, @CreatedAt, @CreatedBy)
            """;
        await connection.ExecuteAsync(sql, user);
        return user.Id;
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE dbo.CFG_Users
            SET RoleId = @RoleId,
                FullName = @FullName,
                Email = @Email,
                IsActive = @IsActive,
                TwoFactorEnabled = @TwoFactorEnabled,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id
            """;
        await connection.ExecuteAsync(sql, user);
    }

    public async Task UpdatePasswordAsync(Guid id, string passwordHash, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE dbo.CFG_Users
            SET PasswordHash = @PasswordHash,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id
            """;
        await connection.ExecuteAsync(sql, new { Id = id, PasswordHash = passwordHash, UpdatedAt = DateTime.UtcNow });
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM dbo.CFG_Users WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task SetMustChangePasswordAsync(Guid id, bool value, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE dbo.CFG_Users
            SET MustChangePassword = @Value,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id
            """;
        await connection.ExecuteAsync(sql, new { Id = id, Value = value, UpdatedAt = DateTime.UtcNow });
    }

    public async Task SetResetTokenAsync(Guid id, string? token, DateTime? expiresAtUtc, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            UPDATE dbo.CFG_Users
            SET ResetToken = @Token,
                ResetTokenExpiresAt = @ExpiresAtUtc
            WHERE Id = @Id
            """;
        await connection.ExecuteAsync(sql, new { Id = id, Token = token, ExpiresAtUtc = expiresAtUtc });
    }

    public async Task<User?> GetByResetTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT Id, LandlordId, RoleId, FullName, Email, PasswordHash, IsOwner, IsActive, TwoFactorEnabled,
                   MustChangePassword, ResetToken, ResetTokenExpiresAt,
                   CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
            FROM dbo.CFG_Users
            WHERE ResetToken = @Token
            """;
        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Token = token });
    }
}
