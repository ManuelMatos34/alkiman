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

    /// <summary>Ver <see cref="CarwashTipMode"/> para los valores posibles.</summary>
    public string TipMode { get; set; } = CarwashTipMode.Optional;

    /// <summary>
    /// Porcentaje del total con el que se pre-carga el campo de propina cuando
    /// <see cref="TipMode"/> es <see cref="CarwashTipMode.Suggested"/>. Se ignora
    /// en los otros dos modos.
    /// </summary>
    public decimal TipSuggestedPercent { get; set; } = 10m;

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

/// <summary>
/// Política de propinas del negocio (<see cref="CarwashSettings.TipMode"/>).
///
/// Ninguno de los tres modos COBRA: el módulo no procesa pagos, el servicio se
/// paga en efectivo en el mostrador. Lo único que cambia entre modos es qué se
/// pregunta al entregar el vehículo. Un cargo obligatorio agregado a todos los
/// tickets sería un aumento de precio con otro nombre, y anotar una propina que
/// nadie entregó ensucia justo el dato que el ranking de lavadores existe para
/// producir.
/// </summary>
public static class CarwashTipMode
{
    /// <summary>El negocio no maneja propinas: al entregar no se pregunta nada.</summary>
    public const string Disabled = "Disabled";

    /// <summary>Se pregunta con el campo vacío. Default: no presupone nada.</summary>
    public const string Optional = "Optional";

    /// <summary>Se pregunta con el campo pre-cargado con <see cref="CarwashSettings.TipSuggestedPercent"/> del total. Sugiere; el cajero igual confirma o corrige.</summary>
    public const string Suggested = "Suggested";

    public static bool IsValid(string? value) =>
        value is Disabled or Optional or Suggested;
}
