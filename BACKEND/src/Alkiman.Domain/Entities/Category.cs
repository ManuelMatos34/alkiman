using Alkiman.Domain.Common;

namespace Alkiman.Domain.Entities;

/// <summary>Agrupa los activos para su correcta organización. Tabla: CFG_Categories.</summary>
public class Category : IAuditable
{
    public int Id { get; set; }
    public Guid LandlordId { get; set; }
    public string Name { get; set; } = default!;

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
