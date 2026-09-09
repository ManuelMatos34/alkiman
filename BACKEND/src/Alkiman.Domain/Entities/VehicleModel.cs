namespace Alkiman.Domain.Entities;

/// <summary>Modelo de vehículo vinculado a una marca. Tabla: VH_Models.</summary>
public class VehicleModel
{
    public int Id { get; set; }
    public int MakeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
