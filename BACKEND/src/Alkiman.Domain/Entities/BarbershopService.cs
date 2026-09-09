using Alkiman.Domain.Common;

namespace Alkiman.Domain.Entities;

/// <summary>Servicio del catálogo de la barbería (ej: "Corte de cabello", "Tinte"). Tabla: BRB_Services.</summary>
public class BarbershopService : IAuditable
{
    public int Id { get; set; }
    public Guid LandlordId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
