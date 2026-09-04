import type { AccentColor, ThemeMode } from "@/domain/types/landlord"
import type { CarwashTicketExtra, CarwashTicketStatus } from "@/domain/types/carwash"

/** Servicio tal como lo ve el cliente público en el link de Carwash. */
export interface CarwashPublicService {
  id: number
  name: string
  description: string | null
  price: number
  estimatedMinutes: number
}

/** Agregado opcional (encerado, ozono, ...) tal como lo ve el cliente público. */
export interface CarwashPublicExtra {
  id: number
  name: string
  description: string | null
  price: number
  estimatedMinutes: number
}

/**
 * Info pública de un link de Carwash: negocio + servicios y extras activos. Incluye la apariencia
 * del negocio dueño del link, igual que `PortalCatalog`, para que la pantalla pública se pinte con
 * SU marca.
 */
export interface CarwashPublicLink {
  businessName: string
  appName: string
  themeMode: ThemeMode
  accentColor: AccentColor
  linkTitle: string
  services: CarwashPublicService[]
  extras: CarwashPublicExtra[]
}

/** Auto-registro del cliente en la cola desde el link público. */
export interface JoinCarwashQueueRequest {
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

/**
 * Estado del turno tal como lo ve el cliente en su link público `/lavado/turno/{token}`. Es
 * también lo que devuelve el join, y de ahí sale el `accessToken` con el que se navega a esa
 * página.
 */
export interface CarwashPublicTicketStatus {
  ticketId: string
  accessToken: string
  queueNumber: number
  status: CarwashTicketStatus
  arrivalDeadline: string | null
  vehiclePlate: string
  serviceName: string
  extras: CarwashTicketExtra[]
  total: number
  businessName: string
  appName: string
  themeMode: ThemeMode
  accentColor: AccentColor
  createdAt: string
}
