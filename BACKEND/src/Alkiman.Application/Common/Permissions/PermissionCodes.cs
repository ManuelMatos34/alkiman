namespace Alkiman.Application.Common.Permissions;

/// <summary>
/// Códigos de permiso usados como claim JWT ("permission") y como nombre de
/// política de autorización registrada en Program.cs. Deben coincidir
/// exactamente con los códigos sembrados en dbo.CFG_Permissions.
/// </summary>
public static class PermissionCodes
{
    public const string AssetsView = "assets.view";
    public const string AssetsManage = "assets.manage";

    public const string CustomersView = "customers.view";
    public const string CustomersManage = "customers.manage";

    public const string RentalsView = "rentals.view";
    public const string RentalsManage = "rentals.manage";

    public const string PaymentsView = "payments.view";
    public const string PaymentsManage = "payments.manage";

    public const string CategoriesView = "categories.view";
    public const string CategoriesManage = "categories.manage";

    public const string AuditView = "audit.view";

    public const string SettingsManage = "settings.manage";

    public const string UsersManage = "users.manage";

    public const string RolesManage = "roles.manage";

    public const string AssetGroupsView = "assetgroups.view";
    public const string AssetGroupsManage = "assetgroups.manage";

    public const string ReportsView = "reports.view";

    public const string EmailsView = "emails.view";
    public const string EmailsManage = "emails.manage";

    public const string PortalView = "portal.view";
    public const string PortalManage = "portal.manage";

    public const string ContractsView = "contracts.view";
    public const string ContractsManage = "contracts.manage";

    public const string RentalRequestsView = "rentalrequests.view";
    public const string RentalRequestsManage = "rentalrequests.manage";

    // --- Carwash: permisos por sección (sin carwash.view / carwash.manage genéricos) ---

    /// <summary>Ver el tablero y la cola de vehículos.</summary>
    public const string CarwashBoardView = "carwash.board.view";

    /// <summary>Avanzar y retroceder el estado de los vehículos en el tablero.</summary>
    public const string CarwashBoardWork = "carwash.board.work";

    /// <summary>Registrar vehículos, asignar lavadores, cancelar y gestionar turnos.</summary>
    public const string CarwashBoardManage = "carwash.board.manage";

    /// <summary>Pantalla de Caja: ver vehículos listos, cobrar propina y marcar Entregado.</summary>
    public const string CarwashCaja = "carwash.caja";

    /// <summary>Ver el catálogo de servicios y agregados.</summary>
    public const string CarwashCatalogView = "carwash.catalog.view";

    /// <summary>Crear, editar y eliminar servicios y agregados del catálogo.</summary>
    public const string CarwashCatalogManage = "carwash.catalog.manage";

    /// <summary>Ver el directorio de lavadores.</summary>
    public const string CarwashWashersView = "carwash.washers.view";

    /// <summary>Agregar y editar lavadores, y configurar la política de propinas.</summary>
    public const string CarwashWashersManage = "carwash.washers.manage";

    /// <summary>Ver los links de portal público del carwash.</summary>
    public const string CarwashPortalView = "carwash.portal.view";

    /// <summary>Crear, activar y eliminar links de portal público.</summary>
    public const string CarwashPortalManage = "carwash.portal.manage";

    /// <summary>Ver métricas, facturación y ranking de lavadores.</summary>
    public const string CarwashReports = "carwash.reports";

    // --- Barbershop: permisos por sección ---

    /// <summary>Ver el tablero de citas.</summary>
    public const string BarbershopBoardView = "barbershop.board.view";

    /// <summary>Avanzar el estado de las citas.</summary>
    public const string BarbershopBoardWork = "barbershop.board.work";

    /// <summary>Crear citas y cancelarlas desde el sistema.</summary>
    public const string BarbershopBoardManage = "barbershop.board.manage";

    /// <summary>Ver el catálogo de servicios.</summary>
    public const string BarbershopCatalogView = "barbershop.catalog.view";

    /// <summary>Crear, editar y eliminar servicios.</summary>
    public const string BarbershopCatalogManage = "barbershop.catalog.manage";

    /// <summary>Ver el directorio de estilistas.</summary>
    public const string BarbershopStylistsView = "barbershop.stylists.view";

    /// <summary>Agregar y editar estilistas.</summary>
    public const string BarbershopStylistsManage = "barbershop.stylists.manage";

    /// <summary>Ver los links de portal público de la barbería.</summary>
    public const string BarbershopPortalView = "barbershop.portal.view";

    /// <summary>Crear, activar y eliminar links de portal público.</summary>
    public const string BarbershopPortalManage = "barbershop.portal.manage";

    /// <summary>Ver métricas y reportes de la barbería.</summary>
    public const string BarbershopReports = "barbershop.reports";
}
