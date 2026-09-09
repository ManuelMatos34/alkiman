namespace Alkiman.Application.Auth;

public record RegisterRequest(string BusinessName, string FullName, string Email, string Password);

public record LoginRequest(string Email, string Password);

public record AuthResponse(
    string Token,
    DateTime ExpiresAtUtc,
    Guid UserId,
    string FullName,
    Guid LandlordId,
    string BusinessName,
    string Email,
    string Role,
    bool IsOwner,
    IReadOnlyList<string> Permissions,
    /// <summary>True si el frontend debe forzar el cambio de contraseña antes de dejar usar el resto de la app (ver UserService.CreateAsync).</summary>
    bool MustChangePassword);

/// <summary>
/// Resultado de <c>POST /api/auth/login</c>. La contraseña correcta ya no alcanza para
/// tener sesión: si el usuario tiene el segundo factor activo, vuelve
/// <c>RequiresTwoFactor = true</c> con un <see cref="ChallengeToken"/> y
/// <see cref="Session"/> en null, y el JWT recién se emite al verificar el código.
///
/// Es un resultado con dos formas en vez de dos endpoints distintos porque quien llama
/// no puede saber de antemano cuál le toca: el frontend no conoce la configuración del
/// usuario hasta después de mandar la contraseña.
/// </summary>
public record LoginResponse(bool RequiresTwoFactor, string? ChallengeToken, AuthResponse? Session);

/// <summary>Segundo paso del login: el código de 6 dígitos que llegó por correo.</summary>
public record VerifyTwoFactorRequest(string ChallengeToken, string Code);

/// <summary>Pide otro código para el mismo desafío (el anterior queda invalidado).</summary>
public record ResendTwoFactorRequest(string ChallengeToken);

/// <summary>Pedido de recuperación de contraseña por email. Nunca revela si el email existe o no.</summary>
public record ForgotPasswordRequest(string Email);

/// <summary>Confirmación de recuperación de contraseña con el token recibido por email.</summary>
public record ResetPasswordRequest(string Token, string NewPassword);
