namespace Alkiman.Application.Common.Modules;

/// <summary>
/// Códigos de los módulos de la plataforma. Refleja 1:1 la columna Code de
/// dbo.CFG_Modules; es la clave que usan CFG_LandlordModules (qué compró cada
/// negocio) y CFG_Permissions.ModuleCode (a qué módulo pertenece cada permiso).
///
/// Existir aquí no significa estar disponible para la venta: eso lo decide
/// CFG_Modules.IsAvailable.
/// </summary>
public static class ModuleCodes
{
    public const string Alquileres = "alquileres";
    public const string Carwash = "carwash";
    public const string Barbershop = "barbershop";
    public const string Citas = "citas";
    public const string Inventario = "inventario";
    public const string Facturacion = "facturacion";
}
