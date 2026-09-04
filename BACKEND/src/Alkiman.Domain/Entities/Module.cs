using Alkiman.Domain.Common;

namespace Alkiman.Domain.Entities;

/// <summary>Catálogo global de módulos de la plataforma. Tabla: CFG_Modules.</summary>
public class Module : IAuditable
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string IconName { get; set; } = default!;
    public bool IsAvailable { get; set; }
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
