using Alkiman.Application.AuditLogs;
using Alkiman.Application.Common.Exceptions;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Modules;
using Alkiman.Application.Permissions;
using Alkiman.Domain.Entities;
using Alkiman.Domain.Enums;

namespace Alkiman.Application.Roles;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _repository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IModuleRepository _moduleRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogService _auditLog;

    public RoleService(
        IRoleRepository repository,
        IPermissionRepository permissionRepository,
        IModuleRepository moduleRepository,
        ICurrentUserService currentUser,
        IAuditLogService auditLog)
    {
        _repository = repository;
        _permissionRepository = permissionRepository;
        _moduleRepository = moduleRepository;
        _currentUser = currentUser;
        _auditLog = auditLog;
    }

    public async Task<IReadOnlyList<RoleResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _repository.GetAllByLandlordAsync(_currentUser.LandlordId, cancellationToken);
        var responses = new List<RoleResponse>(roles.Count);
        foreach (var role in roles)
        {
            responses.Add(await ToResponseAsync(role, cancellationToken));
        }
        return responses;
    }

    public async Task<RoleResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var role = await GetOwnedOrThrowAsync(id, cancellationToken);
        return await ToResponseAsync(role, cancellationToken);
    }

    public async Task<RoleResponse> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 100)
            throw new AppValidationException("El nombre del rol es inválido.");

        var permissionIds = await ResolvePermissionIdsAsync(request.Permissions, cancellationToken);

        var role = new Role
        {
            LandlordId = _currentUser.LandlordId,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsSystem = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId.ToString(),
        };

        role.Id = await _repository.CreateAsync(role, permissionIds, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Create, "CFG_Roles", role.Id.ToString(), null, role, cancellationToken);
        return await ToResponseAsync(role, cancellationToken);
    }

    public async Task<RoleResponse> UpdateAsync(int id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = await GetOwnedOrThrowAsync(id, cancellationToken);

        if (role.IsSystem)
            throw new AppValidationException("El rol Administrador es de sistema y no puede modificarse.");

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 100)
            throw new AppValidationException("El nombre del rol es inválido.");

        var permissionIds = await ResolvePermissionIdsAsync(request.Permissions, cancellationToken);

        role.Name = request.Name.Trim();
        role.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        role.UpdatedAt = DateTime.UtcNow;
        role.UpdatedBy = _currentUser.UserId.ToString();

        await _repository.UpdateAsync(role, permissionIds, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Update, "CFG_Roles", role.Id.ToString(), null, role, cancellationToken);
        return await ToResponseAsync(role, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var role = await GetOwnedOrThrowAsync(id, cancellationToken);

        if (role.IsSystem)
            throw new AppValidationException("El rol Administrador es de sistema y no puede eliminarse.");

        var usersCount = await _repository.CountUsersAsync(id, cancellationToken);
        if (usersCount > 0)
            throw new AppValidationException("No se puede eliminar un rol que tiene usuarios asignados.");

        await _repository.DeleteAsync(id, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Delete, "CFG_Roles", role.Id.ToString(), role, null, cancellationToken);
    }

    /// <summary>
    /// Traduce códigos de permiso a Ids, rechazando los que no existen y los que
    /// pertenecen a un módulo que el negocio no tiene habilitado. Ese segundo filtro
    /// es el que impide armar un rol con permisos de un módulo no comprado mandando
    /// los códigos a mano, aunque el editor ya no los muestre.
    /// </summary>
    private async Task<List<int>> ResolvePermissionIdsAsync(IReadOnlyList<string> codes, CancellationToken cancellationToken)
    {
        var catalog = await _permissionRepository.GetAllAsync(cancellationToken);
        var byCode = catalog.ToDictionary(p => p.Code, StringComparer.OrdinalIgnoreCase);

        var enabledModules = await _moduleRepository.GetEnabledModuleCodesAsync(_currentUser.LandlordId, cancellationToken);
        var enabled = new HashSet<string>(enabledModules, StringComparer.OrdinalIgnoreCase);

        var ids = new List<int>();
        var invalid = new List<string>();
        var notEnabled = new List<string>();
        foreach (var code in codes.Distinct())
        {
            if (!byCode.TryGetValue(code, out var permission))
                invalid.Add(code);
            else if (permission.ModuleCode is not null && !enabled.Contains(permission.ModuleCode))
                notEnabled.Add(code);
            else
                ids.Add(permission.Id);
        }

        if (invalid.Count > 0)
            throw new AppValidationException($"Permisos inválidos: {string.Join(", ", invalid)}.");

        if (notEnabled.Count > 0)
            throw new AppValidationException($"Permisos de módulos no habilitados para este negocio: {string.Join(", ", notEnabled)}.");

        return ids;
    }

    private async Task<Role> GetOwnedOrThrowAsync(int id, CancellationToken cancellationToken)
    {
        var role = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Role), id);

        if (role.LandlordId != _currentUser.LandlordId)
            throw new ForbiddenException("El rol no pertenece al negocio autenticado.");

        return role;
    }

    private async Task<RoleResponse> ToResponseAsync(Role role, CancellationToken cancellationToken)
    {
        var permissions = await _repository.GetPermissionCodesAsync(role.Id, cancellationToken);
        var usersCount = await _repository.CountUsersAsync(role.Id, cancellationToken);
        return new RoleResponse(role.Id, role.Name, role.Description, role.IsSystem, permissions, usersCount, role.CreatedAt);
    }
}
