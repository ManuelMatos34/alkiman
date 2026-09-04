using Alkiman.Domain.Common;

namespace Alkiman.Domain.Entities;

/// <summary>Directorio de inquilinos/clientes que rentan los activos. Tabla: CRM_Customers.</summary>
public class Customer : IAuditable
{
    public Guid Id { get; set; }
    public Guid LandlordId { get; set; }
    public string FullName { get; set; } = default!;
    /// <summary>
    /// Opcional: el portal público de rentas no la pide (el cliente puede auto-rentar
    /// sin cédula/RNC a mano). El alta manual desde el panel sigue exigiéndola,
    /// pero esa validación vive en la capa de aplicación, no en la entidad.
    /// </summary>
    public string? IdentityNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    /// <summary>Recolectados en el paso 1 de la pasarela del portal público.</summary>
    public string? Address { get; set; }
    public string? Country { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
