using Alkiman.Application.Common.Modules;

namespace Alkiman.Application.Common.Permissions;

/// <summary>
/// Un permiso del catálogo estático de la aplicación.
/// </summary>
/// <param name="Code">Código único del permiso; es el valor del claim "permission".</param>
/// <param name="Module">Etiqueta de agrupación visual para el editor de roles.</param>
/// <param name="Description">Texto que ve el usuario en el editor de roles.</param>
/// <param name="ModuleCode">
/// Módulo (CFG_Modules.Code) al que pertenece el permiso, o NULL si es un permiso
/// de plataforma que existe sin importar qué módulos compró el negocio.
/// </param>
public record PermissionCatalogItem(string Code, string Module, string Description, string? ModuleCode);

/// <summary>
/// Catálogo completo y estático de permisos disponibles. Se usa para registrar
/// las políticas de autorización (una por permiso) en Program.cs, y refleja
/// 1:1 el contenido sembrado en dbo.CFG_Permissions.
///
/// ModuleCode es la pertenencia real del permiso (ver 18_CFG_Permission_Modules.sql):
/// los NULL son transversales —administración del negocio y clientes, porque
/// CRM_Customers es compartido entre módulos— y el resto solo aplica si el negocio
/// tiene ese módulo habilitado en CFG_LandlordModules.
/// </summary>
public static class PermissionCatalog
{
    public static readonly IReadOnlyList<PermissionCatalogItem> All = new List<PermissionCatalogItem>
    {
        // --- Plataforma (transversal, sin módulo) ---
        new(PermissionCodes.CustomersView, "Clientes", "Ver clientes", null),
        new(PermissionCodes.CustomersManage, "Clientes", "Crear, editar y eliminar clientes", null),
        new(PermissionCodes.AuditView, "Bitácora", "Ver la bitácora de actividad", null),
        new(PermissionCodes.SettingsManage, "Ajustes", "Editar el perfil y la apariencia del negocio", null),
        new(PermissionCodes.UsersManage, "Usuarios", "Invitar, editar y eliminar usuarios del negocio", null),
        new(PermissionCodes.RolesManage, "Roles", "Crear, editar y eliminar roles y sus permisos", null),

        // --- Alquileres ---
        new(PermissionCodes.AssetsView, "Activos", "Ver activos", ModuleCodes.Alquileres),
        new(PermissionCodes.AssetsManage, "Activos", "Crear, editar y eliminar activos", ModuleCodes.Alquileres),
        new(PermissionCodes.AssetGroupsView, "Grupos de Activos", "Ver grupos de activos", ModuleCodes.Alquileres),
        new(PermissionCodes.AssetGroupsManage, "Grupos de Activos", "Crear, editar y eliminar grupos de activos", ModuleCodes.Alquileres),
        new(PermissionCodes.RentalsView, "Rentas", "Ver rentas", ModuleCodes.Alquileres),
        new(PermissionCodes.RentalsManage, "Rentas", "Crear y editar rentas", ModuleCodes.Alquileres),
        new(PermissionCodes.PaymentsView, "Pagos", "Ver pagos", ModuleCodes.Alquileres),
        new(PermissionCodes.PaymentsManage, "Pagos", "Registrar pagos", ModuleCodes.Alquileres),
        new(PermissionCodes.CategoriesView, "Categorías", "Ver categorías", ModuleCodes.Alquileres),
        new(PermissionCodes.CategoriesManage, "Categorías", "Crear, editar y eliminar categorías", ModuleCodes.Alquileres),
        new(PermissionCodes.ReportsView, "Reportes", "Ver métricas y reportes del negocio", ModuleCodes.Alquileres),
        new(PermissionCodes.EmailsView, "Correos", "Ver correos enviados y recordatorios", ModuleCodes.Alquileres),
        new(PermissionCodes.EmailsManage, "Correos", "Enviar correos y gestionar recordatorios", ModuleCodes.Alquileres),
        new(PermissionCodes.PortalView, "Portal de Rentas", "Ver links del portal de rentas", ModuleCodes.Alquileres),
        new(PermissionCodes.PortalManage, "Portal de Rentas", "Generar y administrar links del portal de rentas", ModuleCodes.Alquileres),
        new(PermissionCodes.ContractsView, "Contratos", "Ver plantillas y contratos generados", ModuleCodes.Alquileres),
        new(PermissionCodes.ContractsManage, "Contratos", "Administrar plantillas de contrato y firmar/reenviar contratos", ModuleCodes.Alquileres),
        new(PermissionCodes.RentalRequestsView, "Pedidos de Renta", "Ver pedidos de prórroga y cancelación de rentas", ModuleCodes.Alquileres),
        new(PermissionCodes.RentalRequestsManage, "Pedidos de Renta", "Aprobar o rechazar pedidos de prórroga y cancelación", ModuleCodes.Alquileres),

        // --- Carwash: Tablero ---
        new(PermissionCodes.CarwashBoardView,   "Carwash · Tablero", "Ver el tablero y la cola de vehículos", ModuleCodes.Carwash),
        new(PermissionCodes.CarwashBoardWork,   "Carwash · Tablero", "Avanzar y retroceder el estado de los vehículos", ModuleCodes.Carwash),
        new(PermissionCodes.CarwashBoardManage, "Carwash · Tablero", "Registrar vehículos, asignar lavadores y cancelar turnos", ModuleCodes.Carwash),

        // --- Carwash: Caja ---
        new(PermissionCodes.CarwashCaja, "Carwash · Caja", "Procesar entregas y cobrar propinas en la caja", ModuleCodes.Carwash),

        // --- Carwash: Catálogo ---
        new(PermissionCodes.CarwashCatalogView,   "Carwash · Catálogo", "Ver servicios y agregados del catálogo", ModuleCodes.Carwash),
        new(PermissionCodes.CarwashCatalogManage, "Carwash · Catálogo", "Crear, editar y eliminar servicios y agregados", ModuleCodes.Carwash),

        // --- Carwash: Lavadores ---
        new(PermissionCodes.CarwashWashersView,   "Carwash · Lavadores", "Ver el directorio de lavadores", ModuleCodes.Carwash),
        new(PermissionCodes.CarwashWashersManage, "Carwash · Lavadores", "Agregar y editar lavadores, configurar propinas", ModuleCodes.Carwash),

        // --- Carwash: Portal ---
        new(PermissionCodes.CarwashPortalView,   "Carwash · Portal", "Ver los links de portal público", ModuleCodes.Carwash),
        new(PermissionCodes.CarwashPortalManage, "Carwash · Portal", "Crear, activar y eliminar links de portal", ModuleCodes.Carwash),

        // --- Carwash: Reportes ---
        new(PermissionCodes.CarwashReports, "Carwash · Reportes", "Ver métricas, facturación y ranking de lavadores", ModuleCodes.Carwash),

        // --- Barbería: Tablero ---
        new(PermissionCodes.BarbershopBoardView,   "Barbería · Tablero", "Ver el tablero de citas", ModuleCodes.Barbershop),
        new(PermissionCodes.BarbershopBoardWork,   "Barbería · Tablero", "Avanzar el estado de las citas", ModuleCodes.Barbershop),
        new(PermissionCodes.BarbershopBoardManage, "Barbería · Tablero", "Crear citas y cancelarlas desde el sistema", ModuleCodes.Barbershop),

        // --- Barbería: Catálogo ---
        new(PermissionCodes.BarbershopCatalogView,   "Barbería · Catálogo", "Ver el catálogo de servicios", ModuleCodes.Barbershop),
        new(PermissionCodes.BarbershopCatalogManage, "Barbería · Catálogo", "Crear, editar y eliminar servicios", ModuleCodes.Barbershop),

        // --- Barbería: Estilistas ---
        new(PermissionCodes.BarbershopStylistsView,   "Barbería · Estilistas", "Ver el directorio de estilistas", ModuleCodes.Barbershop),
        new(PermissionCodes.BarbershopStylistsManage, "Barbería · Estilistas", "Agregar y editar estilistas", ModuleCodes.Barbershop),

        // --- Barbería: Portal ---
        new(PermissionCodes.BarbershopPortalView,   "Barbería · Portal", "Ver los links de portal público", ModuleCodes.Barbershop),
        new(PermissionCodes.BarbershopPortalManage, "Barbería · Portal", "Crear, activar y eliminar links de portal", ModuleCodes.Barbershop),

        // --- Barbería: Reportes ---
        new(PermissionCodes.BarbershopReports, "Barbería · Reportes", "Ver métricas y reportes de la barbería", ModuleCodes.Barbershop),
    };

    public static readonly IReadOnlyList<string> AllCodes = All.Select(p => p.Code).ToList();

    /// <summary>Permisos que pertenecen a un módulo. Lo usa el provisioner al habilitarlo.</summary>
    public static IReadOnlyList<PermissionCatalogItem> ForModule(string moduleCode) =>
        All.Where(p => p.ModuleCode == moduleCode).ToList();
}
