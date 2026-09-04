using Alkiman.Domain.Common;

namespace Alkiman.Domain.Entities;

/// <summary>
/// Catálogo de servicios de lavado por negocio (ej: "Lavado básico", "Lavado +
/// encerado"). Análogo a <see cref="Category"/> pero propio de Carwash. Se
/// llama "ServiceItem" (no "CarwashService") para no chocar con la clase de
/// Application <c>CarwashService</c>. Tabla: CWS_Services.
/// </summary>
public class CarwashServiceItem : IAuditable
{
    public int Id { get; set; }
    public Guid LandlordId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int EstimatedMinutes { get; set; }
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
