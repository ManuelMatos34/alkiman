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

/** `operationMode` en `null` significa que todavía no se eligió modo: hay que mostrar el diálogo inicial. */
export interface CarwashSettings {
  operationMode: CarwashOperationMode | null
}

export interface SaveCarwashSettingsRequest {
  operationMode: CarwashOperationMode
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
 * NO es un usuario del sistema: es una entidad propia de Carwash, el mismo lugar
 * que ocupa `Customer` en Alquileres. La persona que lava no necesita cuenta.
 * `userId` con valor significa que además tiene una, y entonces puede iniciar
 * sesión y mover su propia cola.
 */
export interface CarwashWasher {
  id: string
  fullName: string
  phone: string | null
  isActive: boolean
  userId: string | null
  userEmail: string | null
  /** false si ya tiene turnos: hay que desactivarlo en vez de eliminarlo, para no perder el historial. */
  canDelete: boolean
}

export interface CreateCarwashWasherRequest {
  fullName: string
  phone?: string | null
  userId?: string | null
}

export interface UpdateCarwashWasherRequest {
  fullName: string
  phone?: string | null
  isActive: boolean
  userId?: string | null
}

/** Cuenta del negocio que se le puede vincular a un lavador. */
export interface CarwashLinkableUser {
  id: string
  fullName: string
  email: string
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
}

export interface AssignWasherRequest {
  washerId: string | null
}
