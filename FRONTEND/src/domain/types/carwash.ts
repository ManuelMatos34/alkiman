/** Estados por los que pasa un vehículo en la cola de Carwash. */
export type CarwashTicketStatus =
  | "Waiting"
  | "ArrivalPending"
  | "InProgress"
  | "Drying"
  | "Waxing"
  | "Ready"
  | "Delivered"
  | "Cancelled"
  | "Expired"

/** Origen del registro: lo creó el Encargado presencialmente, o el propio cliente desde el portal. */
export type CarwashTicketSource = "Presencial" | "Portal"

/**
 * Modo de operación del negocio, elegido la primera vez que se entra al módulo.
 * `Empresa`: varios lavadores, el trabajo se asigna explícitamente.
 * `Solitario`: una sola persona, el ticket se auto-asigna al iniciar el lavado.
 */
export type CarwashOperationMode = "Empresa" | "Solitario"

/**
 * Política de propinas del negocio.
 *
 * Ninguno COBRA: Carwash no procesa pagos, el servicio se paga en efectivo en el
 * mostrador. Lo único que cambia entre modos es qué se pregunta al entregar.
 * `Disabled`: no se pregunta.
 * `Optional`: se pregunta con el campo vacío.
 * `Suggested`: se pregunta con el campo pre-cargado con `tipSuggestedPercent`.
 */
export type CarwashTipMode = "Disabled" | "Optional" | "Suggested"

/** `operationMode` en `null` significa que todavía no se eligió modo: hay que mostrar el diálogo inicial. */
export interface CarwashSettings {
  operationMode: CarwashOperationMode | null
  tipMode: CarwashTipMode
  tipSuggestedPercent: number
}

export interface SaveCarwashSettingsRequest {
  operationMode: CarwashOperationMode
  /** Opcional: el diálogo de configuración inicial sólo manda el modo de operación. */
  tipMode?: CarwashTipMode
  tipSuggestedPercent?: number
}

/** Servicio del catálogo de Carwash (ej: "Lavado básico", "Lavado premium"). */
export interface CarwashServiceItem {
  id: number
  name: string
  description: string | null
  price: number
  estimatedMinutes: number
  isActive: boolean
}

export interface CreateCarwashServiceRequest {
  name: string
  description?: string | null
  price: number
  estimatedMinutes: number
}

export interface UpdateCarwashServiceRequest {
  name: string
  description?: string | null
  price: number
  estimatedMinutes: number
  isActive: boolean
}

/** Agregado opcional del catálogo (encerado, ozono, ...) que se suma al servicio base. */
export interface CarwashExtraItem {
  id: number
  name: string
  description: string | null
  price: number
  estimatedMinutes: number
  isActive: boolean
}

export interface CreateCarwashExtraRequest {
  name: string
  description?: string | null
  price: number
  estimatedMinutes: number
}

export interface UpdateCarwashExtraRequest {
  name: string
  description?: string | null
  price: number
  estimatedMinutes: number
  isActive: boolean
}

/** Link público del portal de Carwash (mantenimiento autenticado). */
export interface CarwashPortalLink {
  id: string
  title: string
  slug: string
  isActive: boolean
}

export interface CreateCarwashPortalLinkRequest {
  title: string
}

/** Extra ya aplicado a un ticket: nombre y precio congelados al momento del alta, no los del catálogo actual. */
export interface CarwashTicketExtra {
  extraId: number
  name: string
  price: number
}

/**
 * Un lavador del directorio del módulo.
 *
 * NO es un usuario del sistema y no se puede vincular con uno: es una entidad
 * propia de Carwash, el mismo lugar que ocupa `Customer` en Alquileres. Es una
 * ficha de personal, nada más. Quien además tenga que entrar al software se
 * crea aparte, en Usuarios del sistema, sin relación con esta ficha.
 */
export interface CarwashWasher {
  id: string
  fullName: string
  phone: string | null
  isActive: boolean
  /** false si ya tiene turnos: hay que desactivarlo en vez de eliminarlo, para no perder el historial. */
  canDelete: boolean
}

export interface CreateCarwashWasherRequest {
  fullName: string
  phone?: string | null
}

export interface UpdateCarwashWasherRequest {
  fullName: string
  phone?: string | null
  isActive: boolean
}

/** Vehículo en la cola de Carwash. */
export interface CarwashTicket {
  id: string
  queueNumber: number
  vehiclePlate: string
  vehicleBrand: string | null
  vehicleModel: string | null
  vehicleYear: number | null
  vehicleColor: string | null
  customerId: string
  customerName: string
  customerPhone: string | null
  serviceId: number
  serviceName: string
  servicePrice: number
  extras: CarwashTicketExtra[]
  /** Servicio base + extras, todo a precios congelados. */
  total: number
  assignedToWasherId: string | null
  assignedToName: string | null
  status: CarwashTicketStatus
  source: CarwashTicketSource
  arrivalDeadline: string | null
  notes: string | null
  /** Propina confirmada al entregar. `null` mientras el ticket no esté entregado o si no hubo. */
  tipAmount: number | null
  tipWasherId: string | null
  tipWasherName: string | null
  createdAt: string
}

/** Registro presencial de un vehículo (lo crea el Encargado). */
export interface RegisterCarwashTicketRequest {
  customerName: string
  customerPhone?: string | null
  customerEmail?: string | null
  serviceId: number
  extraIds?: number[] | null
  vehiclePlate: string
  vehicleBrand?: string | null
  vehicleModel?: string | null
  vehicleYear?: number | null
  vehicleColor?: string | null
}

export interface AdvanceCarwashTicketStatusRequest {
  status: CarwashTicketStatus
  /** Sólo se mira al pasar a `Delivered`. Es lo que el mostrador confirma haber recibido, no un cargo. */
  tipAmount?: number | null
}

// ---- Métricas del módulo ----

export interface CarwashVolumeSummary {
  washed: number
  cancelled: number
  /** No-shows del portal: reservaron y no llegaron antes del vencimiento. */
  expired: number
  stillInQueue: number
  /** (cancelados + vencidos) sobre el total de los que salieron de la cola. */
  cancellationRatePercent: number
  averageServiceMinutes: number | null
}

export interface CarwashRevenueSummary {
  servicesRevenue: number
  extrasRevenue: number
  totalRevenue: number
  averageTicket: number
  tipsTotal: number
  /** Porcentaje de entregas con propina: dice si la política está funcionando. */
  tipsCoveragePercent: number
}

export interface CarwashDailyPoint {
  date: string
  washed: number
  revenue: number
}

export interface CarwashServiceUsage {
  serviceId: number
  serviceName: string
  count: number
  revenue: number
}

export interface CarwashExtraUsage {
  extraId: number
  extraName: string
  count: number
  revenue: number
}

export interface CarwashHourlyPoint {
  hour: number
  washed: number
}

export interface CarwashWasherRanking {
  washerId: string
  washerName: string
  isActive: boolean
  washed: number
  revenue: number
  tipsTotal: number
  averageServiceMinutes: number | null
}

export interface CarwashMetrics {
  fromDate: string
  toDate: string
  volume: CarwashVolumeSummary
  revenue: CarwashRevenueSummary
  dailyVolume: CarwashDailyPoint[]
  topServices: CarwashServiceUsage[]
  topExtras: CarwashExtraUsage[]
  hourlyDistribution: CarwashHourlyPoint[]
  washerRanking: CarwashWasherRanking[]
}

export interface AssignWasherRequest {
  washerId: string | null
}
