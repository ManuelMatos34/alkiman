using Alkiman.Domain.Common;
using Alkiman.Domain.Enums;

namespace Alkiman.Domain.Entities;

/// <summary>Tabla híbrida central: modela inmuebles (largo plazo) y objetos/vehículos (corto plazo). Tabla: INV_Assets.</summary>
public class Asset : IAuditable
{
    public Guid Id { get; set; }
    public Guid LandlordId { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public AssetStatus Status { get; set; }
    public RentalTypeOption RentalType { get; set; }
    public decimal BasePrice { get; set; }
    public int Stock { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
