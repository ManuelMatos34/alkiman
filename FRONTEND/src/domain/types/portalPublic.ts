import type { RentalTypeOption } from "@/domain/types/asset"
import type { AccentColor, ThemeMode } from "@/domain/types/landlord"
import type { PortalPaymentProvider } from "@/domain/types/portalPayment"

/** Activo tal como lo ve el cliente público en el catálogo del link. */
export interface PortalAsset {
  id: string
  name: string
  description: string | null
  imageUrl: string | null
  categoryName: string
  basePrice: number
  rentalType: RentalTypeOption
}

/**
 * Catálogo público que ve el cliente al entrar al link (paso 0: elegir activo).
 * Incluye la apariencia (nombre de app, tema, color de acento) del negocio dueño del link,
 * para que la pantalla pública se pinte con SU marca y no una genérica.
 */
export interface PortalCatalog {
  businessName: string
  appName: string
  themeMode: ThemeMode
  accentColor: AccentColor
  linkTitle: string
  assets: PortalAsset[]
}

/** Submit final de la pasarela de 4 pasos (datos personales, detalle de la renta, pago Stripe, firma). */
export interface PortalCheckoutRequest {
  assetId: string
  fullName: string
  email: string
  phone?: string | null
  address?: string | null
  country?: string | null
  startDate: string
  /** Cantidad de períodos (según el rentalType del activo) que el cliente quiere rentar. Mínimo 1. */
  periods: number
  quantity: number
  paymentProvider: PortalPaymentProvider
  /** Stripe: `paymentIntentId`. */
  paymentReference: string
  /** Imagen PNG en Base64 de la firma digital del cliente, sin el prefijo `data:image/png;base64,`. Obligatoria: la generación del contrato en el Portal es inmediata y siempre queda "Firmado". */
  signatureImageBase64: string
}

export interface PortalCheckoutResponse {
  rentalId: string
  customerId: string
  startDate: string
  endDate: string
  totalPrice: number
  paymentProvider: string
  paymentReference: string
}
