using Alkiman.Domain.Common;

namespace Alkiman.Domain.Entities;

/// <summary>
/// Rol definido dentro de un negocio, agrupa un conjunto de permisos.
/// El rol de sistema "Administrador" (IsSystem = true) se crea junto con el
/// negocio y no puede editarse ni eliminarse. Tabla: CFG_Roles.
/// </summary>
public class Role : IAuditable
{
    public int Id { get; set; }
    public Guid LandlordId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
