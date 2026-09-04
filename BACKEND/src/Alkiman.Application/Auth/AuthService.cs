using System.Security.Cryptography;
using Alkiman.Application.Common.Exceptions;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Common.Modules;
using Alkiman.Application.Common.Roles;
using Alkiman.Application.Emails;
using Alkiman.Application.Landlords;
using Alkiman.Application.Modules;
using Alkiman.Application.Permissions;
using Alkiman.Application.Roles;
using Alkiman.Application.Users;
using Alkiman.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace Alkiman.Application.Auth;

public class AuthService : IAuthService
{
    private const string OwnerRoleName = SystemRoleNames.Owner;

    /// <summary>Módulos con los que arranca todo negocio nuevo.</summary>
    private static readonly string[] InitialModules = [ModuleCodes.Alquileres];

    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromHours(1);

    private readonly ILandlordRepository _landlordRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IModuleRepository _moduleRepository;
    private readonly IModuleProvisioner _moduleProvisioner;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;

    public AuthService(
        ILandlordRepository landlordRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IModuleRepository moduleRepository,
        IModuleProvisioner moduleProvisioner,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        IEmailSender emailSender,
        IConfiguration configuration)
    {
        _landlordRepository = landlordRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _moduleRepository = moduleRepository;
        _moduleProvisioner = moduleProvisioner;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _emailSender = emailSender;
        _configuration = configuration;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.BusinessName))
            throw new AppValidationException("El nombre del negocio es obligatorio.");

        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new AppValidationException("El nombre completo es obligatorio.");

        if (request.Password.Length < 8)
            throw new AppValidationException("La contraseña debe tener al menos 8 caracteres.");

        var existing = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
            throw new AppValidationException("Ya existe una cuenta registrada con este email.");

        var landlordId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var createdBy = userId.ToString();

        var landlord = new Landlord
        {
            Id = landlordId,
            BusinessName = request.BusinessName.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
        };
        await _landlordRepository.CreateAsync(landlord, cancellationToken);

        // El Administrador nace solo con los permisos de PLATAFORMA (ModuleCode null):
        // ajustes, usuarios, roles, bitácora y clientes. Lo de cada módulo se lo entrega
        // el provisioner más abajo, el mismo camino que corre cuando el negocio compra un
        // módulo después. Antes se le daba el catálogo entero, así que arrancaba con
        // permisos de módulos que nunca compró.
        var allPermissions = await _permissionRepository.GetAllAsync(cancellationToken);
        var permissions = allPermissions.Where(p => p.ModuleCode is null).ToList();

        var role = new Role
        {
            LandlordId = landlordId,
            Name = OwnerRoleName,
            Description = "Rol de propietario con acceso total al negocio.",
            IsSystem = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
        };
        var roleId = await _roleRepository.CreateAsync(role, permissions.Select(p => p.Id), cancellationToken);

        // Después de crear el rol, porque el provisioner le suma los permisos del módulo
        // y necesita encontrarlo.
        foreach (var moduleCode in InitialModules)
        {
            await _moduleRepository.EnableModuleAsync(landlordId, moduleCode, createdBy, cancellationToken);
            await _moduleProvisioner.ProvisionAsync(landlordId, moduleCode, createdBy, cancellationToken);
        }

        var user = new User
        {
            Id = userId,
            LandlordId = landlordId,
            RoleId = roleId,
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            IsOwner = true,
            IsActive = true,
            TwoFactorEnabled = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
        };
        await _userRepository.CreateAsync(user, cancellationToken);

        // Se releen del rol y no de la lista de arriba: el provisioner ya le agregó los
        // permisos de los módulos iniciales, y el token tiene que salir con todos.
        var permissionCodes = await _roleRepository.GetPermissionCodesAsync(roleId, cancellationToken);
        var (token, expiresAtUtc) = _tokenGenerator.GenerateToken(
            user.Id, landlordId, user.Email, user.FullName, landlord.BusinessName, OwnerRoleName, true, permissionCodes);

        // El dueño elige su propia contraseña al registrarse: nunca queda forzado a cambiarla.
        return new AuthResponse(
            token, expiresAtUtc, user.Id, user.FullName, landlordId, landlord.BusinessName, user.Email, OwnerRoleName, true, permissionCodes, false);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new AppValidationException("Email o contraseña incorrectos.");

        return await BuildSessionAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        return await BuildSessionAsync(user, cancellationToken);
    }

    /// <summary>
    /// Arma la sesión (token + datos del usuario) leyendo rol y permisos de la base.
    /// Lo comparten login y refresh para que un token reemitido salga idéntico a uno
    /// recién logueado, incluido el chequeo de usuario desactivado.
    /// </summary>
    private async Task<AuthResponse> BuildSessionAsync(User user, CancellationToken cancellationToken)
    {
        if (!user.IsActive)
            throw new AppValidationException("El usuario está desactivado.");

        var landlord = await _landlordRepository.GetByIdAsync(user.LandlordId, cancellationToken)
            ?? throw new NotFoundException(nameof(Landlord), user.LandlordId);

        var role = await _roleRepository.GetByIdAsync(user.RoleId, cancellationToken);
        var permissionCodes = role is null
            ? new List<string>()
            : (await _roleRepository.GetPermissionCodesAsync(role.Id, cancellationToken)).ToList();

        var (token, expiresAtUtc) = _tokenGenerator.GenerateToken(
            user.Id, landlord.Id, user.Email, user.FullName, landlord.BusinessName, role?.Name ?? "—", user.IsOwner, permissionCodes);

        return new AuthResponse(
            token, expiresAtUtc, user.Id, user.FullName, landlord.Id, landlord.BusinessName, user.Email, role?.Name ?? "—", user.IsOwner, permissionCodes,
            user.MustChangePassword);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null)
            return; // No revelamos si el email existe o no: la respuesta es siempre la misma.

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var expiresAtUtc = DateTime.UtcNow.Add(ResetTokenLifetime);
        await _userRepository.SetResetTokenAsync(user.Id, token, expiresAtUtc, cancellationToken);

        await TrySendResetPasswordEmailAsync(user, token, cancellationToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (request.NewPassword.Length < 8)
            throw new AppValidationException("La contraseña debe tener al menos 8 caracteres.");

        var user = await _userRepository.GetByResetTokenAsync(request.Token, cancellationToken);
        if (user is null || user.ResetTokenExpiresAt is null || user.ResetTokenExpiresAt < DateTime.UtcNow)
            throw new AppValidationException("El link de recuperación es inválido o venció. Solicita uno nuevo.");

        await _userRepository.UpdatePasswordAsync(user.Id, _passwordHasher.Hash(request.NewPassword), cancellationToken);
        // Un reset exitoso ya satisface la exigencia de "cambiar contraseña" pendiente, si había una.
        await _userRepository.SetMustChangePasswordAsync(user.Id, false, cancellationToken);
        await _userRepository.SetResetTokenAsync(user.Id, null, null, cancellationToken);
    }

    /// <summary>Envía el link de recuperación de contraseña. Nunca lanza: si falla, el token ya quedó guardado y el usuario puede reintentar el pedido.</summary>
    private async Task TrySendResetPasswordEmailAsync(User user, string token, CancellationToken cancellationToken)
    {
        try
        {
            var baseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
            var resetLink = $"{baseUrl}/restablecer-password?token={token}";

            const string subject = "Restablece tu contraseña de Alkiman";
            var body =
                $"Hola {user.FullName}, recibimos un pedido para restablecer tu contraseña.\n\n" +
                $"Si fuiste tú, haz clic en este link (vence en 1 hora): {resetLink}\n\n" +
                "Si no fuiste tú, puedes ignorar este correo.";

            await _emailSender.SendAsync(user.Email, user.FullName, subject, body, cancellationToken);
        }
        catch
        {
            // Best-effort: el token ya quedó guardado, el usuario puede pedir el link de nuevo.
        }
    }
}
