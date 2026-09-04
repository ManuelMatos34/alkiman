using Alkiman.Domain.Common;

namespace Alkiman.Domain.Entities;

/// <summary>
/// Plantilla de contrato de un negocio, asociada a una Categoría: cuando se crea una
/// renta de un activo de esa categoría, la plantilla activa de la categoría es la que
/// se usa para generar el <see cref="Contract"/> de esa renta. Un negocio puede tener
/// varias plantillas por categoría (historial de versiones), pero solo una activa a la
/// vez por categoría (ver ContractTemplateService). Tabla: COM_ContractTemplates.
/// </summary>
public class ContractTemplate : IAuditable
{
    public int Id { get; set; }
    public Guid LandlordId { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = default!;
    /// <summary>
    /// Contenido de la plantilla en texto plano, con placeholders tipo {{ClienteNombre}}
    /// que ContractService reemplaza al generar el contrato de una renta concreta.
    /// El contenido detallado/legal se termina de definir más adelante; por ahora es
    /// texto libre editable desde el mantenimiento de contratos.
    /// </summary>
    public string Content { get; set; } = default!;
    /// <summary>Si es la plantilla que se usa actualmente para nuevas rentas de su categoría.</summary>
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
