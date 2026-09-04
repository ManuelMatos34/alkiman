namespace Alkiman.Application.Users;

public record UserResponse(
    Guid Id,
    string FullName,
    string Email,
    bool IsActive,
    bool IsOwner,
    bool TwoFactorEnabled,
    int RoleId,
    string RoleName,
    DateTime CreatedAt);

public record CreateUserRequest(string FullName, string Email, int RoleId);

/// <summary>
/// Respuesta del alta de un usuario.
///
/// La contraseña temporal NO viaja acá a propósito: sólo la conoce su destinatario,
/// por email. Que el admin pueda leerla convierte una credencial personal en un
/// secreto compartido y anula la trazabilidad de "quién entró con esa cuenta".
///
/// <see cref="WelcomeEmailSent"/> reemplaza ese respaldo: si el envío falla, el
/// admin se entera y puede pedirle a la persona que use "olvidé mi contraseña",
/// en vez de que el alta quede en un silencio del que nadie se entera.
/// </summary>
public record CreateUserResponse(
    Guid Id,
    string FullName,
    string Email,
    bool IsActive,
    bool IsOwner,
    bool TwoFactorEnabled,
    int RoleId,
    string RoleName,
    DateTime CreatedAt,
    bool WelcomeEmailSent);

public record UpdateUserRequest(string FullName, int RoleId, bool IsActive);
