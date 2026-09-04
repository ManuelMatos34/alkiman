namespace Alkiman.Application.Auth;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reemite el JWT del usuario con su rol y permisos actuales, sin pedir credenciales.
    ///
    /// Los permisos viajan como claims dentro del token, así que quedan congelados al
    /// momento de emitirlo: si el negocio habilita un módulo, el rol Administrador recibe
    /// los permisos nuevos en la base pero el token en curso sigue sin ellos y la API
    /// responde 403 hasta el próximo login. Esto evita ese bache.
    ///
    /// No extiende una sesión inválida: se llama con el token vigente y vuelve a validar
    /// que el usuario exista y esté activo.
    /// </summary>
    Task<AuthResponse> RefreshAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Genera y envía (best-effort) un link de recuperación de contraseña si el email
    /// pertenece a un usuario. Nunca revela si el email existe: siempre completa sin error.
    /// </summary>
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>Confirma la recuperación de contraseña validando el token de un solo uso y su vencimiento.</summary>
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}
