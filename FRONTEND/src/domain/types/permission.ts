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

  // Carwash: Tablero
  /** Ver el tablero y la cola de vehículos. */
  CarwashBoardView: "carwash.board.view",
  /** Avanzar y retroceder el estado de los vehículos. */
  CarwashBoardWork: "carwash.board.work",
  /** Registrar vehículos, asignar lavadores y cancelar turnos. */
  CarwashBoardManage: "carwash.board.manage",

  // Carwash: Caja
  /** Procesar entregas y cobrar propinas. */
  CarwashCaja: "carwash.caja",

  // Carwash: Catálogo
  /** Ver servicios y agregados. */
  CarwashCatalogView: "carwash.catalog.view",
  /** Crear, editar y eliminar servicios y agregados. */
  CarwashCatalogManage: "carwash.catalog.manage",

  // Carwash: Lavadores
  /** Ver el directorio de lavadores. */
  CarwashWashersView: "carwash.washers.view",
  /** Agregar y editar lavadores, configurar propinas. */
  CarwashWashersManage: "carwash.washers.manage",

  // Carwash: Portal
  /** Ver los links de portal público. */
  CarwashPortalView: "carwash.portal.view",
  /** Crear, activar y eliminar links de portal. */
  CarwashPortalManage: "carwash.portal.manage",

  // Carwash: Reportes
  /** Ver métricas, facturación y ranking de lavadores. */
  CarwashReports: "carwash.reports",

  // Barbería: Tablero
  BarbershopBoardView:   "barbershop.board.view",
  BarbershopBoardWork:   "barbershop.board.work",
  BarbershopBoardManage: "barbershop.board.manage",

  // Barbería: Catálogo
  BarbershopCatalogView:   "barbershop.catalog.view",
  BarbershopCatalogManage: "barbershop.catalog.manage",

  // Barbería: Estilistas
  BarbershopStylistsView:   "barbershop.stylists.view",
  BarbershopStylistsManage: "barbershop.stylists.manage",

  // Barbería: Portal
  BarbershopPortalView:   "barbershop.portal.view",
  BarbershopPortalManage: "barbershop.portal.manage",

  // Barbería: Reportes
  BarbershopReports: "barbershop.reports",
} as const

export type PermissionCode = (typeof PermissionCodes)[keyof typeof PermissionCodes]
