namespace Alkiman.Application.Payments.Gateways;

/// <summary>
/// Puerto de aplicación hacia Stripe (modo sandbox/test). La implementación real vive en
/// Infrastructure (<c>Alkiman.Infrastructure.Payments.StripeGateway</c>), usando el SDK
/// oficial Stripe.net.
/// </summary>
public interface IStripeGateway
{
    /// <summary>True si <c>Stripe:SecretKey</c> está configurado en <see cref="Microsoft.Extensions.Configuration.IConfiguration"/>.</summary>
    bool IsConfigured { get; }

    /// <summary>Clave pública configurada (<c>Stripe:PublishableKey</c>), o cadena vacía si no está configurada.</summary>
    string PublishableKey { get; }

    /// <summary>
    /// Crea un PaymentIntent por <paramref name="amountInCents"/> centavos en la moneda indicada
    /// (ver nota de conversión de moneda en <see cref="Portal.PortalPaymentService"/>).
    /// </summary>
    Task<StripePaymentIntentResult> CreatePaymentIntentAsync(long amountInCents, string currency, CancellationToken cancellationToken = default);

    /// <summary>Consulta el estado actual de un PaymentIntent ya creado, para verificarlo server-side antes de confirmar la renta.</summary>
    Task<StripePaymentIntentStatus> GetPaymentIntentAsync(string paymentIntentId, CancellationToken cancellationToken = default);
}

/// <summary>Resultado de crear un PaymentIntent: lo que el frontend necesita para confirmar el pago con Stripe.js/Elements.</summary>
public record StripePaymentIntentResult(string PaymentIntentId, string ClientSecret);

/// <summary>
/// Estado verificado de un PaymentIntent existente. <paramref name="AmountInCents"/> es el monto
/// tal como quedó registrado en Stripe (para comparar contra el precio recalculado server-side).
/// </summary>
public record StripePaymentIntentStatus(string PaymentIntentId, string Status, long AmountInCents, string Currency);
