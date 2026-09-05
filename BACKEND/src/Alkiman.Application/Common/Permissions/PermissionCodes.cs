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

    public const string CarwashView = "carwash.view";
    public const string CarwashManage = "carwash.manage";

    /// <summary>Avanzar el estado de los vehículos en la cola. Separado de <see cref="CarwashManage"/> para que el rol "Lavador" pueda mover la cola sin poder administrar el catálogo ni los links públicos.</summary>
    public const string CarwashWork = "carwash.work";

    /// <summary>Métricas, facturación y ranking de lavadores. Separado de <see cref="CarwashView"/> por el mismo motivo que <see cref="ReportsView"/> lo está en Alquileres: ver la cola del día es operativo, ver cuánto factura el negocio y quién rinde más no lo es.</summary>
    public const string CarwashReports = "carwash.reports";
}
