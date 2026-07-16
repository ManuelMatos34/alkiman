using Alkiman.Domain.Common;

namespace Alkiman.Domain.Entities;

/// <summary>Directorio de inquilinos/clientes que rentan los activos. Tabla: CRM_Customers.</summary>
public class Customer : IAuditable
{
    public Guid Id { get; set; }
    public Guid LandlordId { get; set; }
    public string FullName { get; set; } = default!;
    public string IdentityNumber { get; set; } = default!;
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
