using Alkiman.Domain.Common;

namespace Alkiman.Domain.Entities;

/// <summary>Estilista del negocio. Entidad del módulo, no una identidad de sistema (sin vínculo con CFG_Users). Tabla: BRB_Stylists.</summary>
public class BarbershopStylist : IAuditable
{
    public Guid Id { get; set; }
    public Guid LandlordId { get; set; }
    public string FullName { get; set; } = default!;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
