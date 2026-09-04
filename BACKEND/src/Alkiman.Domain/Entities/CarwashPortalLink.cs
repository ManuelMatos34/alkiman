using Alkiman.Domain.Common;

namespace Alkiman.Domain.Entities;

/// <summary>
/// Link público (sin login) por negocio para que un cliente se auto-registre
/// en la cola de Carwash. Mismo patrón que <see cref="PortalLink"/> (Slug
/// único), pero sin AssetGroup: acá no aplica el concepto de grupo de
/// activos. Tabla: CWS_PortalLinks.
/// </summary>
public class CarwashPortalLink : IAuditable
{
    public Guid Id { get; set; }
    public Guid LandlordId { get; set; }
    public string Title { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
