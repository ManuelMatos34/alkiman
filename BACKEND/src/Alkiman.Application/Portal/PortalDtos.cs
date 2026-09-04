using Alkiman.Domain.Enums;

namespace Alkiman.Application.Portal;

/// <summary>
/// Catálogo público que ve el cliente al entrar al link (paso 0 de la pasarela: elegir activo).
/// Incluye la apariencia configurada por el negocio dueño del link (AppName/ThemeMode/AccentColor)
/// para que el frontend pueda pintar la página pública con la marca de ESE negocio, no una genérica.
/// </summary>
public record PortalCatalogResponse(
    string BusinessName,
    string AppName,
    string ThemeMode,
    string AccentColor,
    string LinkTitle,
    IReadOnlyList<PortalAssetResponse> Assets);

public record PortalAssetResponse(
    Guid Id,
    string Name,
    string? Description,
    string? ImageUrl,
    string CategoryName,
    decimal BasePrice,
    RentalTypeOption RentalType);

/// <summary>
/// Envío final de la pasarela de 3 pasos: datos personales (paso 1), detalle de la
/// renta (paso 2) y tarjeta (paso 3), todo en un solo submit.
/// </summary>
/// <param name="Periods">
/// Cantidad de períodos (según el <see cref="RentalTypeOption"/> del activo, ej: meses si es
/// Monthly, semanas si es Weekly) que el cliente quiere rentar. Mínimo 1: es la forma en que se
/// aplica la regla de "no se puede rentar menos que la unidad del activo". La fecha de fin y el
/// precio total los calcula siempre el servidor a partir de este valor, nunca se confía en un
/// EndDate/TotalPrice enviado por el cliente.
/// </param>
/// <param name="SignatureImageBase64">
/// Firma digital que el cliente dibuja en el paso final de la pasarela (imagen PNG en Base64,
/// sin el prefijo "data:image/png;base64,"). Con ella el contrato generado para esta renta
/// queda "Firmado" de una vez; si viene vacía, el contrato se genera igual pero "Pendiente"
/// de firma (se puede firmar luego desde el mantenimiento de Contratos).
/// </param>
/// <param name="PaymentProvider">"Stripe": qué proveedor procesó el pago (ver <see cref="Alkiman.Application.Payments.Gateways"/>).</param>
/// <param name="PaymentReference">
/// Referencia del pago a re-verificar server-side antes de crear la renta: el PaymentIntentId
/// devuelto por <c>POST .../payment/stripe/intent</c> (Stripe).
/// </param>
public record PortalCheckoutRequest(
    Guid AssetId,
    string FullName,
    string Email,
    string? Phone,
    string? Address,
    string? Country,
    DateTime StartDate,
    int Periods,
    int Quantity,
    string PaymentProvider,
    string PaymentReference,
    string? SignatureImageBase64);

public record PortalCheckoutResponse(
    Guid RentalId,
    Guid CustomerId,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalPrice,
    string PaymentProvider,
    string PaymentReference);

/// <summary>Vista previa del contrato ANTES de firmar/pagar: el cliente la ve en el paso 3 de la pasarela (ver PortalContractPreviewResponse.Content) para poder leer y aceptar los términos antes de firmar y pagar. No persiste nada.</summary>
public record PortalContractPreviewRequest(
    Guid AssetId,
    string FullName,
    string? IdentityNumber,
    string? Email,
    string? Phone,
    string? Address,
    DateTime StartDate,
    int Periods,
    int Quantity);

public record PortalContractPreviewResponse(
    string Content,
    DateTime EndDate,
    decimal TotalPrice);
