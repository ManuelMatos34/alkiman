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

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
