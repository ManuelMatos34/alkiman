using Alkiman.Application.Payments.Gateways;
using Microsoft.Extensions.Configuration;
using Stripe;

namespace Alkiman.Infrastructure.Payments;

/// <summary>
/// Implementación real de <see cref="IStripeGateway"/> con el SDK oficial Stripe.net, en modo
/// SANDBOX/TEST (se usa la Secret Key de test de la cuenta Stripe, nunca una live). Se instancia
/// un <see cref="StripeClient"/> propio a partir de la key leída de configuración en vez de usar
/// la API estática vieja (<c>StripeConfiguration.ApiKey</c> global), para que sea thread-safe e
/// inyectable/testeable.
/// </summary>
public class StripeGateway : IStripeGateway
{
    private readonly string _secretKey;
    private readonly string _publishableKey;

    public StripeGateway(IConfiguration configuration)
    {
        _secretKey = configuration["Stripe:SecretKey"] ?? string.Empty;
        _publishableKey = configuration["Stripe:PublishableKey"] ?? string.Empty;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_secretKey);

    public string PublishableKey => _publishableKey;

    public async Task<StripePaymentIntentResult> CreatePaymentIntentAsync(long amountInCents, string currency, CancellationToken cancellationToken = default)
    {
        var client = new StripeClient(_secretKey);
        var service = new PaymentIntentService(client);
        var options = new PaymentIntentCreateOptions
        {
            Amount = amountInCents,
            Currency = currency,
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true }
        };
        var intent = await service.CreateAsync(options, cancellationToken: cancellationToken);
        return new StripePaymentIntentResult(intent.Id, intent.ClientSecret);
    }

    public async Task<StripePaymentIntentStatus> GetPaymentIntentAsync(string paymentIntentId, CancellationToken cancellationToken = default)
    {
        var client = new StripeClient(_secretKey);
        var service = new PaymentIntentService(client);
        var intent = await service.GetAsync(paymentIntentId, cancellationToken: cancellationToken);
        return new StripePaymentIntentStatus(intent.Id, intent.Status, intent.Amount, intent.Currency);
    }
}
