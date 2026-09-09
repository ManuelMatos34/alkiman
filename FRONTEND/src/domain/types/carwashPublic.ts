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
  /** Política de propinas del negocio: decide si el paso de propina existe y con qué monto arranca. */
  tipMode: CarwashTipMode
  tipSuggestedPercent: number
  /**
   * false si el negocio no tiene Stripe configurado. La pasarela entonces omite
   * el paso de pago y el turno se paga en el mostrador, como uno presencial.
   */
  paymentEnabled: boolean
}

/** Ver `CarwashTipMode` en el backend. */
export type CarwashTipMode = "Disabled" | "Optional" | "Suggested"

/**
 * Auto-registro del cliente en la cola desde el link público.
 *
 * Los tres últimos campos sólo viajan cuando el turno se pagó online. El
 * servidor no los toma por buenos: recalcula el total y lo contrasta contra lo
 * que Stripe dice que realmente se cobró.
 */
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
  tipAmount?: number | null
  paymentProvider?: string | null
  paymentReference?: string | null
}

/** Config de pago del link público (espeja la del Portal de Rentas). */
export interface CarwashPublicPaymentConfig {
  /** null si el negocio no cobra en línea. */
  stripePublishableKey: string | null
  currency: string
}

/** Pedido de PaymentIntent: nunca lleva el total, ese lo calcula el servidor. */
export interface CarwashPaymentIntentRequest {
  serviceId: number
  extraIds?: number[] | null
  tipAmount?: number | null
}

/** `amount` es el total autoritativo del servidor: es lo que se va a cobrar. */
export interface CarwashStripeIntent {
  paymentIntentId: string
  clientSecret: string
  amount: number
  currency: string
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
