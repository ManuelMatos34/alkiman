using Alkiman.Domain.Common;

namespace Alkiman.Domain.Entities;

/// <summary>
/// Persona que inicia sesión en la plataforma. Pertenece a un negocio (Landlord)
/// y tiene un rol asignado. Tabla: CFG_Users.
/// </summary>
public class User : IAuditable
{
    public Guid Id { get; set; }
    public Guid LandlordId { get; set; }
    public int RoleId { get; set; }
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;

    /// <summary>El usuario dueño del negocio: no puede eliminarse, desactivarse ni cambiar de rol.</summary>
    public bool IsOwner { get; set; }
    public bool IsActive { get; set; } = true;
    public bool TwoFactorEnabled { get; set; }

    /// <summary>
    /// Obliga a cambiar la contraseña en el próximo login: se activa cuando un admin
    /// crea el usuario con una contraseña generada por el sistema (no la eligió la
    /// persona), y se apaga apenas cambia su propia contraseña (o la restablece por
    /// email). El dueño creado en el registro nunca queda con esto en true.
    /// </summary>
    public bool MustChangePassword { get; set; }

    /// <summary>Token de un solo uso para el flujo "olvidé mi contraseña" (null si no hay uno pendiente).</summary>
    public string? ResetToken { get; set; }

    /// <summary>Vencimiento del <see cref="ResetToken"/> vigente.</summary>
    public DateTime? ResetTokenExpiresAt { get; set; }

    /// <summary>
    /// Identificador opaco del desafío de segundo factor en curso, o null si no hay
    /// ninguno. Se lo entrega el login al frontend cuando la contraseña fue correcta
    /// pero falta el código, y el frontend lo devuelve al verificar. Evita tener que
    /// volver a mandar la contraseña en el segundo paso.
    ///
    /// Sólo puede haber uno vivo por usuario: pedir un código nuevo pisa el anterior.
    /// </summary>
    public string? TwoFactorChallengeToken { get; set; }

    /// <summary>
    /// Hash PBKDF2 del código de 6 dígitos enviado por correo. El código en claro no
    /// se guarda nunca; sólo viaja en el correo.
    /// </summary>
    public string? TwoFactorCodeHash { get; set; }

    /// <summary>Vencimiento del código actual. Vencido equivale a inexistente.</summary>
    public DateTime? TwoFactorCodeExpiresAt { get; set; }

    /// <summary>
    /// Intentos fallidos contra el desafío actual. Al llegar al tope el desafío se
    /// descarta y hay que empezar el login de nuevo: seis dígitos sin límite de
    /// intentos son un millón de combinaciones que un script agota rápido.
    /// </summary>
    public int TwoFactorAttempts { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
