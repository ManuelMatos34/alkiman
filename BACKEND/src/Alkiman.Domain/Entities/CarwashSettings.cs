using Alkiman.Domain.Common;

namespace Alkiman.Domain.Entities;

/// <summary>
/// Configuración del módulo Carwash de un negocio. Una fila por negocio
/// (LandlordId es la clave). Que NO exista fila significa "todavía no eligió
/// modo de operación", y es lo que dispara el diálogo de configuración
/// inicial la primera vez que se entra al módulo.
/// Tabla: CWS_Settings.
/// </summary>
public class CarwashSettings : IAuditable
{
    public Guid LandlordId { get; set; }
    /// <summary>Ver <see cref="CarwashOperationMode"/> para los valores posibles.</summary>
    public string OperationMode { get; set; } = default!;

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Modos de operación posibles de <see cref="CarwashSettings.OperationMode"/>
/// (columna NVARCHAR + CHECK constraint, mismo criterio que
/// <see cref="CarwashTicketStatus"/>).
/// </summary>
public static class CarwashOperationMode
{
    /// <summary>Local con varios lavadores: los tickets se asignan explícitamente a un usuario.</summary>
    public const string Empresa = "Empresa";

    /// <summary>Una sola persona atiende todo: el ticket se auto-asigna a quien inicia el lavado y no se muestra el selector de lavador.</summary>
    public const string Solitario = "Solitario";
}
