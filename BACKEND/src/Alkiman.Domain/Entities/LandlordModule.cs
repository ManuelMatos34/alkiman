using Alkiman.Domain.Common;

namespace Alkiman.Domain.Entities;

/// <summary>Módulos habilitados para un negocio. Tabla: CFG_LandlordModules.</summary>
public class LandlordModule : IAuditable
{
    public Guid Id { get; set; }
    public Guid LandlordId { get; set; }
    public string ModuleCode { get; set; } = default!;
    public DateTime EnabledAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
