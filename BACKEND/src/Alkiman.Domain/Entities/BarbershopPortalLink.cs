using Alkiman.Domain.Common;

namespace Alkiman.Domain.Entities;

/// <summary>Link público para que un cliente agende una cita sin login. Puede estar asociado a un estilista específico. Tabla: BRB_PortalLinks.</summary>
public class BarbershopPortalLink : IAuditable
{
    public Guid Id { get; set; }
    public Guid LandlordId { get; set; }
    public Guid? StylistId { get; set; }
    public string Title { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
