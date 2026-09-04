export interface Permission {
  id: number
  code: string
  /** Etiqueta de agrupación visual en el editor de roles ("Activos", "Carwash"). */
  module: string
  /**
   * Módulo al que pertenece el permiso, o null si es de plataforma (existe siempre).
   * El backend ya filtra el catálogo a los módulos habilitados; viaja igual para que
   * la UI pueda mostrar de dónde sale cada permiso.
   */
  moduleCode: string | null
  description: string
}

/// Códigos de permiso usados como claim JWT ("permission") y como nombre de
/// política de autorización en el backend. Deben coincidir exactamente con
/// los códigos sembrados en dbo.CFG_Permissions.
export const PermissionCodes = {
  AssetsView: "assets.view",
  AssetsManage: "assets.manage",

  CustomersView: "customers.view",
  CustomersManage: "customers.manage",

  RentalsView: "rentals.view",
  RentalsManage: "rentals.manage",

  PaymentsView: "payments.view",
  PaymentsManage: "payments.manage",

  CategoriesView: "categories.view",
  CategoriesManage: "categories.manage",

  AuditView: "audit.view",

  SettingsManage: "settings.manage",

  UsersManage: "users.manage",

  RolesManage: "roles.manage",

  AssetGroupsView: "assetgroups.view",
  AssetGroupsManage: "assetgroups.manage",

  ReportsView: "reports.view",

  EmailsView: "emails.view",
  EmailsManage: "emails.manage",

  PortalView: "portal.view",
  PortalManage: "portal.manage",

  ContractsView: "contracts.view",
  ContractsManage: "contracts.manage",

  RentalRequestsView: "rentalrequests.view",
  RentalRequestsManage: "rentalrequests.manage",

  CarwashView: "carwash.view",
  CarwashManage: "carwash.manage",
  /** Avanzar vehículos en la cola. Lo tiene el rol Lavador, que NO puede administrar el catálogo. */
  CarwashWork: "carwash.work",
} as const

export type PermissionCode = (typeof PermissionCodes)[keyof typeof PermissionCodes]
