namespace Alkiman.Application.Portal;

/// <summary>Casos de uso públicos (sin autenticación) de la pasarela de pago sandbox del Portal de Rentas.</summary>
public interface IPortalPaymentService
{
    /// <summary>Claves públicas/estado de configuración de Stripe, para inicializar el checkout en el frontend.</summary>
    Task<PortalPaymentConfigResponse> GetConfigAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Crea un Stripe PaymentIntent por el precio recalculado server-side.</summary>
    Task<StripeIntentResponse> CreateStripeIntentAsync(string slug, PortalPaymentStartRequest request, CancellationToken cancellationToken = default);
}
