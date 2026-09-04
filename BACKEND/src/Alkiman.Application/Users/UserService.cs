using System.Security.Cryptography;
using Alkiman.Application.AuditLogs;
using Alkiman.Application.Common.Exceptions;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Emails;
using Alkiman.Application.Roles;
using Alkiman.Domain.Entities;
using Alkiman.Domain.Enums;

namespace Alkiman.Application.Users;

public class UserService : IUserService
{
    /// <summary>Alfabeto sin caracteres ambiguos (sin 0/O/1/l/I) para la contraseña generada.</summary>
    private const string PasswordChars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%&*";
    private const int GeneratedPasswordLength = 12;

    private readonly IUserRepository _repository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogService _auditLog;
    private readonly IEmailSender _emailSender;

    public UserService(
        IUserRepository repository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        ICurrentUserService currentUser,
        IAuditLogService auditLog,
        IEmailSender emailSender)
    {
        _repository = repository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
        _auditLog = auditLog;
        _emailSender = emailSender;
    }

    public async Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _repository.GetAllByLandlordAsync(_currentUser.LandlordId, cancellationToken);
        var roles = await _roleRepository.GetAllByLandlordAsync(_currentUser.LandlordId, cancellationToken);
        var roleNames = roles.ToDictionary(r => r.Id, r => r.Name);

        return users
            .Select(u => ToResponse(u, roleNames.GetValueOrDefault(u.RoleId, "—")))
            .ToList();
    }

    public async Task<UserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await GetOwnedOrThrowAsync(id, cancellationToken);
        var role = await _roleRepository.GetByIdAsync(user.RoleId, cancellationToken);
        return ToResponse(user, role?.Name ?? "—");
    }

    public async Task<CreateUserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new AppValidationException("El nombre completo es obligatorio.");

        var existing = await _repository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
            throw new AppValidationException("Ya existe un usuario con ese email.");

        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Role), request.RoleId);
        if (role.LandlordId != _currentUser.LandlordId)
            throw new ForbiddenException("El rol no pertenece al negocio autenticado.");

        // El admin ya no elige la contraseña del nuevo usuario: se genera acá y se le
        // exige cambiarla en su primer login (MustChangePassword), tal como se le exige
        // a cualquier persona que recibe una contraseña temporal.
        var generatedPassword = GenerateRandomPassword();

        var user = new User
        {
            Id = Guid.NewGuid(),
            LandlordId = _currentUser.LandlordId,
            RoleId = request.RoleId,
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            PasswordHash = _passwordHasher.Hash(generatedPassword),
            IsOwner = false,
            IsActive = true,
            TwoFactorEnabled = false,
            MustChangePassword = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId.ToString(),
        };

        await _repository.CreateAsync(user, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Create, "CFG_Users", user.Id.ToString(), null, user, cancellationToken);

        // La contraseña generada muere acá: se entrega por email y no vuelve al admin.
        var welcomeEmailSent = await TrySendWelcomeEmailAsync(user, generatedPassword, cancellationToken);

        return new CreateUserResponse(
            user.Id, user.FullName, user.Email, user.IsActive, user.IsOwner, user.TwoFactorEnabled,
            user.RoleId, role.Name, user.CreatedAt, welcomeEmailSent);
    }

    public async Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetOwnedOrThrowAsync(id, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new AppValidationException("El nombre completo es obligatorio.");

        if (user.IsOwner && (request.RoleId != user.RoleId || !request.IsActive))
            throw new AppValidationException("El propietario de la cuenta no puede cambiar de rol ni desactivarse.");

        if (id == _currentUser.UserId && !request.IsActive)
            throw new AppValidationException("No se puede desactivar el propio usuario.");

        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Role), request.RoleId);
        if (role.LandlordId != _currentUser.LandlordId)
            throw new ForbiddenException("El rol no pertenece al negocio autenticado.");

        user.FullName = request.FullName.Trim();
        user.RoleId = request.RoleId;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = _currentUser.UserId.ToString();

        await _repository.UpdateAsync(user, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Update, "CFG_Users", user.Id.ToString(), null, user, cancellationToken);
        return ToResponse(user, role.Name);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await GetOwnedOrThrowAsync(id, cancellationToken);

        if (user.IsOwner)
            throw new AppValidationException("No se puede eliminar al propietario de la cuenta.");

        if (id == _currentUser.UserId)
            throw new AppValidationException("No se puede eliminar el propio usuario.");

        await _repository.DeleteAsync(id, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Delete, "CFG_Users", user.Id.ToString(), user, null, cancellationToken);
    }

    private async Task<User> GetOwnedOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(User), id);

        if (user.LandlordId != _currentUser.LandlordId)
            throw new ForbiddenException("El usuario no pertenece al negocio autenticado.");

        return user;
    }

    private static UserResponse ToResponse(User user, string roleName) =>
        new(user.Id, user.FullName, user.Email, user.IsActive, user.IsOwner, user.TwoFactorEnabled, user.RoleId, roleName, user.CreatedAt);

    /// <summary>Genera una contraseña aleatoria criptográficamente segura para el alta de un usuario.</summary>
    private static string GenerateRandomPassword()
    {
        var bytes = RandomNumberGenerator.GetBytes(GeneratedPasswordLength);
        var result = new char[GeneratedPasswordLength];
        for (var i = 0; i < GeneratedPasswordLength; i++)
        {
            result[i] = PasswordChars[bytes[i] % PasswordChars.Length];
        }
        return new string(result);
    }

    /// <summary>
    /// Envía la contraseña temporal a su dueño. Devuelve si el correo salió: el alta
    /// no se revierte por un fallo de email, pero el admin tiene que enterarse para
    /// poder decirle a la persona que use "olvidé mi contraseña".
    /// </summary>
    private async Task<bool> TrySendWelcomeEmailAsync(User user, string generatedPassword, CancellationToken cancellationToken)
    {
        try
        {
            const string subject = "Tu acceso a Alkiman";
            var body =
                $"Hola {user.FullName}, se creó tu usuario en Alkiman.\n\n" +
                $"Email: {user.Email}\n" +
                $"Contraseña temporal: {generatedPassword}\n\n" +
                "Al iniciar sesión por primera vez te vamos a pedir que la cambies por una propia.";
            await _emailSender.SendAsync(user.Email, user.FullName, subject, body, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
