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

/// <summary>Pedido de recuperación de contraseña por email. Nunca revela si el email existe o no.</summary>
public record ForgotPasswordRequest(string Email);

/// <summary>Confirmación de recuperación de contraseña con el token recibido por email.</summary>
public record ResetPasswordRequest(string Token, string NewPassword);
