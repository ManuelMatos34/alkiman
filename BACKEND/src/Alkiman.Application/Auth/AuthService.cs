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

    /// <summary>
    /// Módulos con los que arranca todo negocio nuevo. Tiene que ser un módulo
    /// disponible en el catálogo: darle de alta uno con IsAvailable = 0 le dejaría
    /// la fila de habilitación pero ninguna pantalla a la que entrar.
    /// </summary>
    private static readonly string[] InitialModules = [ModuleCodes.Carwash];

    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromHours(1);

    /// <summary>
    /// Vigencia del código de doble factor. Corto a propósito: es el tiempo que una
    /// casilla de correo comprometida sirve para entrar.
    /// </summary>
    private static readonly TimeSpan TwoFactorCodeLifetime = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Intentos permitidos por desafío antes de descartarlo. Con 6 dígitos y 5 intentos,
    /// la chance de acertar a ciegas es 1 en 200.000.
    /// </summary>
    private const int MaxTwoFactorAttempts = 5;

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
            Phone1 = request.Phone,
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
            await _moduleProvisioner.ProvisionAsync(landlordId, moduleCode, cancellationToken);
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

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new AppValidationException("Email o contraseña incorrectos.");

        if (!user.TwoFactorEnabled)
            return new LoginResponse(false, null, await BuildSessionAsync(user, cancellationToken));

        // El usuario desactivado se rechaza ANTES de mandar el código: no tiene sentido
        // hacerle atravesar el segundo factor para negarle la sesión al final.
        if (!user.IsActive)
            throw new AppValidationException("El usuario está desactivado.");

        var challengeToken = await StartTwoFactorChallengeAsync(user, cancellationToken);
        return new LoginResponse(true, challengeToken, null);
    }

    public async Task<AuthResponse> VerifyTwoFactorAsync(VerifyTwoFactorRequest request, CancellationToken cancellationToken = default)
    {
        // Mensaje único para "token inexistente", "vencido" y "código equivocado": el
        // que prueba tokens al azar no debe poder distinguir cuál de los tres pasó.
        const string invalidMessage = "El código es incorrecto o venció. Inicia sesión nuevamente.";

        if (string.IsNullOrWhiteSpace(request.ChallengeToken) || string.IsNullOrWhiteSpace(request.Code))
            throw new AppValidationException(invalidMessage);

        var user = await _userRepository.GetByTwoFactorChallengeTokenAsync(request.ChallengeToken, cancellationToken);
        if (user is null || user.TwoFactorCodeHash is null || user.TwoFactorCodeExpiresAt is null)
            throw new AppValidationException(invalidMessage);

        if (user.TwoFactorCodeExpiresAt < DateTime.UtcNow)
        {
            await ClearTwoFactorChallengeAsync(user.Id, cancellationToken);
            throw new AppValidationException(invalidMessage);
        }

        if (!_passwordHasher.Verify(request.Code.Trim(), user.TwoFactorCodeHash))
        {
            var attempts = user.TwoFactorAttempts + 1;
            if (attempts >= MaxTwoFactorAttempts)
            {
                // Se quema el desafío entero, no sólo el intento: si no, el atacante
                // pediría un código nuevo y seguiría probando de a cinco para siempre.
                await ClearTwoFactorChallengeAsync(user.Id, cancellationToken);
                throw new AppValidationException("Demasiados intentos fallidos. Inicia sesión nuevamente.");
            }

            await _userRepository.SetTwoFactorChallengeAsync(
                user.Id, user.TwoFactorChallengeToken, user.TwoFactorCodeHash, user.TwoFactorCodeExpiresAt, attempts, cancellationToken);

            // El último intento cae en singular ("te queda 1 intento"). Es una tontería
            // gramatical, pero es el mensaje que el usuario lee justo antes de quedarse
            // afuera, y ahí conviene que suene escrito por una persona.
            var remaining = MaxTwoFactorAttempts - attempts;
            throw new AppValidationException(remaining == 1
                ? "El código es incorrecto. Te queda 1 intento."
                : $"El código es incorrecto. Te quedan {remaining} intentos.");
        }

        // Un código es de un solo uso: se borra antes de emitir el token para que el
        // mismo correo no sirva dos veces.
        await ClearTwoFactorChallengeAsync(user.Id, cancellationToken);
        return await BuildSessionAsync(user, cancellationToken);
    }

    public async Task ResendTwoFactorAsync(ResendTwoFactorRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ChallengeToken))
            throw new AppValidationException("El desafío venció. Inicia sesión nuevamente.");

        var user = await _userRepository.GetByTwoFactorChallengeTokenAsync(request.ChallengeToken, cancellationToken);
        if (user is null)
            throw new AppValidationException("El desafío venció. Inicia sesión nuevamente.");

        // Reusa el mismo token de desafío: el frontend ya lo tiene y no habría cómo
        // devolvérselo actualizado sin exponerlo en la respuesta del reenvío.
        await StartTwoFactorChallengeAsync(user, cancellationToken, user.TwoFactorChallengeToken);
    }

    /// <summary>
    /// Crea (o renueva) el desafío de segundo factor: genera un código de 6 dígitos, lo
    /// guarda hasheado y lo manda por correo. Devuelve el token de desafío.
    ///
    /// Si el correo no sale, el desafío se descarta y la operación falla. La alternativa
    /// —dejar pasar al usuario sin segundo factor cuando el correo está caído— convertiría
    /// una falla de infraestructura en un bypass de seguridad.
    /// </summary>
    private async Task<string> StartTwoFactorChallengeAsync(User user, CancellationToken cancellationToken, string? reuseChallengeToken = null)
    {
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var challengeToken = reuseChallengeToken ?? Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var expiresAtUtc = DateTime.UtcNow.Add(TwoFactorCodeLifetime);

        await _userRepository.SetTwoFactorChallengeAsync(
            user.Id, challengeToken, _passwordHasher.Hash(code), expiresAtUtc, 0, cancellationToken);

        var minutes = (int)TwoFactorCodeLifetime.TotalMinutes;
        var subject = $"{code} es tu código de acceso a Alkiman";
        var body = EmailTemplate.Build(
            title: "Verificación de identidad",
            greeting: $"Hola {user.FullName},",
            paragraphs:
            [
                $"Tu código de acceso vence en <strong>{minutes} minutos</strong> y solo puede usarse una vez.",
                "Si no intentaste iniciar sesión, alguien conoce tu contraseña — cámbiala cuanto antes."
            ],
            highlightCode: code,
            highlightLabel: "Código de acceso");

        EmailSendResult result;
        try
        {
            result = await _emailSender.SendAsync(user.Email, user.FullName, subject, body, cancellationToken);
        }
        catch (Exception ex)
        {
            await ClearTwoFactorChallengeAsync(user.Id, cancellationToken);
            throw new AppValidationException($"No pudimos enviarte el código de verificación: {ex.Message}");
        }

        if (!result.Success)
        {
            await ClearTwoFactorChallengeAsync(user.Id, cancellationToken);
            throw new AppValidationException(
                $"No pudimos enviarte el código de verificación: {result.ErrorMessage ?? "el envío de correo no está configurado."}");
        }

        return challengeToken;
    }

    private Task ClearTwoFactorChallengeAsync(Guid userId, CancellationToken cancellationToken)
        => _userRepository.SetTwoFactorChallengeAsync(userId, null, null, null, 0, cancellationToken);

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
            var body = EmailTemplate.Build(
                title: "Restablecimiento de contraseña",
                greeting: $"Hola {user.FullName},",
                paragraphs:
                [
                    "Recibimos un pedido para restablecer tu contraseña. Si fuiste tú, usa el botón de abajo — el link vence en <strong>1 hora</strong>.",
                    "Si no fuiste tú, puedes ignorar este correo. Tu contraseña no cambiará."
                ],
                ctaLabel: "Restablecer contraseña",
                ctaUrl: resetLink);

            await _emailSender.SendAsync(user.Email, user.FullName, subject, body, cancellationToken);
        }
        catch
        {
            // Best-effort: el token ya quedó guardado, el usuario puede pedir el link de nuevo.
        }
    }
}
