namespace Alkiman.Domain.Entities;

/// <summary>
/// Permiso del catálogo global del sistema (fijo, no editable desde la UI).
/// Tabla: CFG_Permissions.
/// </summary>
public class Permission
{
    public int Id { get; set; }
    public string Code { get; set; } = default!;

    /// <summary>Etiqueta de agrupación visual para el editor de roles ("Activos", "Carwash").</summary>
    public string Module { get; set; } = default!;

    /// <summary>
    /// Módulo al que pertenece el permiso (FK a CFG_Modules.Code). NULL = permiso
    /// de plataforma: existe siempre, sin importar qué módulos compró el negocio.
    /// </summary>
    public string? ModuleCode { get; set; }

    public string Description { get; set; } = default!;
}
