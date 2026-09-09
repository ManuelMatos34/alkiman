namespace Alkiman.Domain.Entities;

/// <summary>Marca de vehículo del catálogo. Tabla: VH_Makes.</summary>
public class VehicleMake
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
