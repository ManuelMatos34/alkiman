using Alkiman.Domain.Common;

namespace Alkiman.Domain.Entities;

/// <summary>Bloqueo manual de fechas sobre un activo sin cliente/contrato (RF-B2). Tabla: INV_AssetBlocks.</summary>
public class AssetBlock : IAuditable
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
