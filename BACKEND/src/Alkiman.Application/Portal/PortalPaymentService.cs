using Alkiman.Application.AssetGroups;
using Alkiman.Application.Assets;
using Alkiman.Application.Common.Exceptions;
using Alkiman.Application.Payments.Gateways;
using Alkiman.Domain.Common;
using Alkiman.Domain.Entities;
using Alkiman.Domain.Enums;

namespace Alkiman.Application.Portal;

/// <summary>
/// Orquesta la pasarela de pago sandbox (Stripe) del Portal de Rentas: config pública y
/// creación de PaymentIntent. La verificación server-to-server final (¿el pago realmente se
/// completó?) vive en <see cref="PortalService.CheckoutAsync"/>, que es quien efectivamente
/// crea la renta — este servicio solo inicia el cobro.
///
/// NOTA DE MONEDA: Stripe no soporta DOP (peso dominicano) en modo sandbox/test. Como esto es
/// explícitamente no-productivo, se usa el MISMO valor numérico del precio calculado en DOP
/// pero rotulado como moneda "USD" al llamar al gateway (sin inventar una tasa de cambio falsa
/// que aparente ser una conversión real). Es un placeholder intencional hasta que se defina la
/// moneda/procesador de producción.
/// </summary>
public class PortalPaymentService : IPortalPaymentService
{
    public const string SandboxCurrency = "USD";

    private readonly IPortalRepository _portalRepository;
    private readonly IAssetGroupRepository _assetGroupRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IStripeGateway _stripeGateway;

    public PortalPaymentService(
        IPortalRepository portalRepository,
        IAssetGroupRepository assetGroupRepository,
        IAssetRepository assetRepository,
        IStripeGateway stripeGateway)
    {
        _portalRepository = portalRepository;
        _assetGroupRepository = assetGroupRepository;
        _assetRepository = assetRepository;
        _stripeGateway = stripeGateway;
    }

    public Task<PortalPaymentConfigResponse> GetConfigAsync(string slug, CancellationToken cancellationToken = default)
        // No se valida el link acá a propósito: el frontend debe poder cargar la página de
        // checkout (y este endpoint) ANTES de que existan credenciales reales configuradas.
        => Task.FromResult(new PortalPaymentConfigResponse(_stripeGateway.PublishableKey, SandboxCurrency));

    public async Task<StripeIntentResponse> CreateStripeIntentAsync(string slug, PortalPaymentStartRequest request, CancellationToken cancellationToken = default)
    {
        if (!_stripeGateway.IsConfigured)
            throw new AppValidationException("Stripe no está configurado todavía. Contacta al administrador del sitio.");

        var (_, _, totalPrice) = await ValidateAndCalculatePriceAsync(slug, request, cancellationToken);

        var amountInCents = ToGatewayAmountInCents(totalPrice);
        var result = await _stripeGateway.CreatePaymentIntentAsync(amountInCents, "usd", cancellationToken);

        return new StripeIntentResponse(result.PaymentIntentId, result.ClientSecret, totalPrice, SandboxCurrency);
    }

    /// <summary>
    /// Convierte un monto decimal (en DOP, aunque rotulado como USD en modo sandbox; ver nota de
    /// clase) a centavos enteros, la unidad que espera la API de Stripe.
    /// </summary>
    internal static long ToGatewayAmountInCents(decimal amount)
        => (long)Math.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Repite la validación de <see cref="PortalService.CheckoutAsync"/> (link activo, activo
    /// pertenece al grupo del link, activo disponible, mínimo de períodos) para los endpoints que
    /// inician un cobro, ANTES de que exista una renta. Se duplica a propósito en vez de refactorizar
    /// el checkout existente, para no arriesgar romper ese flujo ya probado.
    /// </summary>
    private async Task<(PortalLink Link, Asset Asset, decimal TotalPrice)> ValidateAndCalculatePriceAsync(
        string slug, PortalPaymentStartRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new NotFoundException(nameof(PortalLink), slug);

        var link = await _portalRepository.GetActiveLinkBySlugAsync(slug.Trim(), cancellationToken)
            ?? throw new NotFoundException(nameof(PortalLink), slug);

        if (request.Quantity < 1)
            throw new AppValidationException("La cantidad debe ser al menos 1.");

        var memberAssetIds = await _assetGroupRepository.GetMemberAssetIdsAsync(link.AssetGroupId, cancellationToken);
        if (!memberAssetIds.Contains(request.AssetId))
            throw new AppValidationException("El activo seleccionado no pertenece a este portal.");

        var asset = await _assetRepository.GetByIdAsync(request.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), request.AssetId);
        if (asset.Status != AssetStatus.Available)
            throw new AppValidationException("El activo seleccionado ya no está disponible.");

        if (request.Periods < RentalPeriodCalculator.MinimumPeriods)
            throw new AppValidationException(
                $"Para este activo la renta mínima es de {RentalPeriodCalculator.MinimumPeriods} período.");

        var totalPrice = RentalPeriodCalculator.CalculateTotalPrice(asset.BasePrice, request.Periods, request.Quantity);
        return (link, asset, totalPrice);
    }
}
