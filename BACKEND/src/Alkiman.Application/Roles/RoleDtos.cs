namespace Alkiman.Application.Roles;

public record RoleResponse(
    int Id,
    string Name,
    string? Description,
    bool IsSystem,
    IReadOnlyList<string> Permissions,
    int UsersCount,
    DateTime CreatedAt);

public record CreateRoleRequest(string Name, string? Description, IReadOnlyList<string> Permissions);

public record UpdateRoleRequest(string Name, string? Description, IReadOnlyList<string> Permissions);
