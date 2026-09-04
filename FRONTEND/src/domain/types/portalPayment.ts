/** Proveedor de pago soportado por la pasarela pública del Portal de Rentas (modo sandbox/test). */
export type PortalPaymentProvider = "Stripe"

/**
 * Config pública de pagos de un link del Portal. Si el negocio todavía no configuró las
 * credenciales sandbox de Stripe, el campo correspondiente viene en `""` y la UI debe
 * deshabilitar/ocultar el pago en vez de romper.
 */
export interface PortalPaymentConfig {
  stripePublishableKey: string
  currency: "USD"
}

/** Body común a la creación de intent/orden: identifica el activo y el detalle de la renta a cobrar. */
export interface PortalPaymentQuoteRequest {
  assetId: string
  periods: number
  quantity: number
}

export interface CreateStripeIntentResponse {
  paymentIntentId: string
  clientSecret: string
  amount: number
  currency: "USD"
}

/** Pago confirmado del lado del proveedor, guardado en el estado del wizard hasta el submit final. */
export interface ConfirmedPortalPayment {
  provider: PortalPaymentProvider
  reference: string
}
