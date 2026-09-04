using Alkiman.Domain.Common;

namespace Alkiman.Domain.Entities;

/// <summary>
/// Agrupa activos (de cualquier categoría) bajo un nombre libre definido por el
/// negocio (ej. "Herramientas", "Edificio A - Apartamentos"). Tabla: INV_AssetGroups.
/// La membresía de activos vive en INV_AssetGroupAssets (N:N con INV_Assets).
/// </summary>
public class AssetGroup : IAuditable
{
    public int Id { get; set; }
    public Guid LandlordId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
