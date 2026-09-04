using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Roles;
using Alkiman.Domain.Entities;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RoleRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Role>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT Id, LandlordId, Name, Description, IsSystem, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
            FROM dbo.CFG_Roles
            WHERE LandlordId = @LandlordId
            ORDER BY Name
            """;
        var result = await connection.QueryAsync<Role>(sql, new { LandlordId = landlordId });
        return result.ToList();
    }

    public async Task<Role?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT Id, LandlordId, Name, Description, IsSystem, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
            FROM dbo.CFG_Roles
            WHERE Id = @Id
            """;
        return await connection.QuerySingleOrDefaultAsync<Role>(sql, new { Id = id });
    }

    public async Task<IReadOnlyList<string>> GetPermissionCodesAsync(int roleId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT p.Code
            FROM dbo.CFG_RolePermissions rp
            INNER JOIN dbo.CFG_Permissions p ON p.Id = rp.PermissionId
            WHERE rp.RoleId = @RoleId
            ORDER BY p.Code
            """;
        var result = await connection.QueryAsync<string>(sql, new { RoleId = roleId });
        return result.ToList();
    }

    public async Task<int> CreateAsync(Role role, IEnumerable<int> permissionIds, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        const string insertRoleSql = """
            INSERT INTO dbo.CFG_Roles (LandlordId, Name, Description, IsSystem, CreatedAt, CreatedBy)
            OUTPUT INSERTED.Id
            VALUES (@LandlordId, @Name, @Description, @IsSystem, @CreatedAt, @CreatedBy)
            """;
        var roleId = await connection.ExecuteScalarAsync<int>(insertRoleSql, role, transaction);

        const string insertPermissionSql = """
            INSERT INTO dbo.CFG_RolePermissions (RoleId, PermissionId) VALUES (@RoleId, @PermissionId)
            """;
        var rows = permissionIds.Select(permissionId => new { RoleId = roleId, PermissionId = permissionId });
        await connection.ExecuteAsync(insertPermissionSql, rows, transaction);

        transaction.Commit();
        return roleId;
    }

    public async Task UpdateAsync(Role role, IEnumerable<int> permissionIds, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        const string updateRoleSql = """
            UPDATE dbo.CFG_Roles
            SET Name = @Name,
                Description = @Description,
                UpdatedAt = @UpdatedAt,
                UpdatedBy = @UpdatedBy
            WHERE Id = @Id
            """;
        await connection.ExecuteAsync(updateRoleSql, role, transaction);

        const string deletePermissionsSql = "DELETE FROM dbo.CFG_RolePermissions WHERE RoleId = @RoleId";
        await connection.ExecuteAsync(deletePermissionsSql, new { RoleId = role.Id }, transaction);

        const string insertPermissionSql = """
            INSERT INTO dbo.CFG_RolePermissions (RoleId, PermissionId) VALUES (@RoleId, @PermissionId)
            """;
        var rows = permissionIds.Select(permissionId => new { RoleId = role.Id, PermissionId = permissionId });
        await connection.ExecuteAsync(insertPermissionSql, rows, transaction);

        transaction.Commit();
    }

    public async Task AddPermissionsAsync(int roleId, IEnumerable<int> permissionIds, CancellationToken cancellationToken = default)
    {
        var ids = permissionIds.Distinct().ToList();
        if (ids.Count == 0)
            return;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        // NOT EXISTS en vez de un DELETE+INSERT: el rol puede tener ya parte de estos
        // permisos y no queremos perder los del resto de sus módulos.
        const string sql = """
            INSERT INTO dbo.CFG_RolePermissions (RoleId, PermissionId)
            SELECT @RoleId, @PermissionId
            WHERE NOT EXISTS (
                SELECT 1 FROM dbo.CFG_RolePermissions
                WHERE RoleId = @RoleId AND PermissionId = @PermissionId
            )
            """;
        var rows = ids.Select(permissionId => new { RoleId = roleId, PermissionId = permissionId });
        await connection.ExecuteAsync(sql, rows);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "DELETE FROM dbo.CFG_Roles WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<int> CountUsersAsync(int roleId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = "SELECT COUNT(*) FROM dbo.CFG_Users WHERE RoleId = @RoleId";
        return await connection.ExecuteScalarAsync<int>(sql, new { RoleId = roleId });
    }
}
