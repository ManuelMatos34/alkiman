using Alkiman.Domain.Common;

namespace Alkiman.Domain.Entities;

/// <summary>Propietario que administra su negocio de alquiler en la plataforma. Tabla: CFG_Landlords.</summary>
public class Landlord : IAuditable
{
    public Guid Id { get; set; }
    public string Auth0UserId { get; set; } = default!;
    public string BusinessName { get; set; } = default!;
    public string Email { get; set; } = default!;

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
