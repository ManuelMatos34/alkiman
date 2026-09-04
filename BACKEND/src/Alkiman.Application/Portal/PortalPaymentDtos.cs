namespace Alkiman.Application.Portal;

/// <summary>
/// Config pública que necesita el frontend para inicializar el widget de Stripe en la
/// página de checkout. Se sirve ANTES de que existan credenciales reales: los campos vienen
/// vacíos ("") si el proveedor todavía no está configurado en <c>IConfiguration</c>, en vez de
/// fallar, para que el checkout pueda cargar y avisar al usuario que el proveedor no está
/// disponible todavía.
/// </summary>
public record PortalPaymentConfigResponse(string StripePublishableKey, string Currency);

/// <summary>Request común a los endpoints que inician un pago (Stripe intent): qué activo, por cuántos períodos y unidades.</summary>
public record PortalPaymentStartRequest(Guid AssetId, int Periods, int Quantity);

public record StripeIntentResponse(string PaymentIntentId, string ClientSecret, decimal Amount, string Currency);
