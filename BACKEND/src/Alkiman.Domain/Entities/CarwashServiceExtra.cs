using Alkiman.Domain.Common;

namespace Alkiman.Domain.Entities;

/// <summary>
/// Agregado opcional que se suma a un servicio de lavado (ej: "Encerado",
/// "Ozono"). Existe como catálogo aparte de <see cref="CarwashServiceItem"/>
/// para no tener que dar de alta un servicio por cada combinación posible
/// ("Lavado + encerado", "Lavado + encerado + ozono", ...).
/// Tabla: CWS_ServiceExtras.
/// </summary>
public class CarwashServiceExtra : IAuditable
{
    public int Id { get; set; }
    public Guid LandlordId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    /// <summary>Minutos que el extra le suma al tiempo estimado del servicio base.</summary>
    public int EstimatedMinutes { get; set; }
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
