using Alkiman.Application.Assets;
using Alkiman.Application.AssetGroups;
using Alkiman.Application.Common.Exceptions;
using Alkiman.Application.Contracts;
using Alkiman.Application.Customers;
using Alkiman.Application.Landlords;
using Alkiman.Application.Payments;
using Alkiman.Application.Payments.Gateways;
using Alkiman.Application.Rentals;
using Alkiman.Domain.Common;
using Alkiman.Domain.Entities;
using Alkiman.Domain.Enums;

namespace Alkiman.Application.Portal;

/// <summary>
/// Flujos públicos (sin autenticación) del Portal de Rentas: el cliente entra con el
/// Slug de un <see cref="PortalLink"/>, ve el catálogo de activos del grupo asociado y,
/// si le interesa, completa el checkout. El checkout crea (o reutiliza, matcheando por
/// Email) al cliente y crea la renta, igual que hace <see cref="Alkiman.Application.RentalImports.RentalImportService"/>
/// para las importaciones masivas, pero para un único registro autoservido.
///
/// El pago (Stripe, modo sandbox/test — ver <see cref="PortalPaymentService"/> para cómo se
/// inicia) se re-verifica server-to-server contra el proveedor ANTES de crear la renta: nunca
/// se confía en que el frontend diga "el pago fue exitoso".
/// </summary>
public class PortalService : IPortalService
{
    private const string PortalCreatedBy = "portal";

    private readonly IPortalRepository _portalRepository;
    private readonly ILandlordRepository _landlordRepository;
    private readonly IAssetGroupRepository _assetGroupRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IRentalRepository _rentalRepository;
    private readonly IContractService _contractService;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IStripeGateway _stripeGateway;

    public PortalService(
        IPortalRepository portalRepository,
        ILandlordRepository landlordRepository,
        IAssetGroupRepository assetGroupRepository,
        IAssetRepository assetRepository,
        ICustomerRepository customerRepository,
        IRentalRepository rentalRepository,
        IContractService contractService,
        IPaymentRepository paymentRepository,
        IStripeGateway stripeGateway)
    {
        _portalRepository = portalRepository;
        _landlordRepository = landlordRepository;
        _assetGroupRepository = assetGroupRepository;
        _assetRepository = assetRepository;
        _customerRepository = customerRepository;
        _rentalRepository = rentalRepository;
        _contractService = contractService;
        _paymentRepository = paymentRepository;
        _stripeGateway = stripeGateway;
    }

    public async Task<PortalCatalogResponse> GetCatalogAsync(string slug, CancellationToken cancellationToken = default)
    {
        var link = await GetActiveLinkOrThrowAsync(slug, cancellationToken);

        var landlord = await _landlordRepository.GetByIdAsync(link.LandlordId, cancellationToken);
        var assets = await _portalRepository.GetCatalogAssetsAsync(link.AssetGroupId, cancellationToken);

        return new PortalCatalogResponse(
            landlord?.BusinessName ?? "Alkiman",
            landlord?.AppName ?? "Alkiman",
            landlord?.ThemeMode ?? "light",
            landlord?.AccentColor ?? "blue",
            link.Title,
            assets);
    }

    public async Task<PortalCheckoutResponse> CheckoutAsync(string slug, PortalCheckoutRequest request, CancellationToken cancellationToken = default)
    {
        var link = await GetActiveLinkOrThrowAsync(slug, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new AppValidationException("El nombre es obligatorio.");
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new AppValidationException("El correo es obligatorio.");
        if (request.Quantity < 1)
            throw new AppValidationException("La cantidad debe ser al menos 1.");
        if (string.IsNullOrWhiteSpace(request.SignatureImageBase64))
            throw new AppValidationException("La firma digital es obligatoria para completar la renta.");

        var memberAssetIds = await _assetGroupRepository.GetMemberAssetIdsAsync(link.AssetGroupId, cancellationToken);
        if (!memberAssetIds.Contains(request.AssetId))
            throw new AppValidationException("El activo seleccionado no pertenece a este portal.");

        var asset = await _assetRepository.GetByIdAsync(request.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), request.AssetId);
        if (asset.Status != AssetStatus.Available)
            throw new AppValidationException("El activo seleccionado ya no está disponible.");

        // El mínimo de 1 período es, justamente, la regla de negocio pedida: el cliente no
        // puede rentar menos que la unidad de tiempo configurada en el activo (ej: si el
        // activo es Monthly, no puede rentar menos de 1 mes).
        if (request.Periods < RentalPeriodCalculator.MinimumPeriods)
            throw new AppValidationException(
                $"Para este activo la renta mínima es de {RentalPeriodCalculator.MinimumPeriods} período ({RentalTypeLabel(asset.RentalType)}).");

        // El servidor es la única fuente de verdad para la fecha de fin y el precio: nunca se
        // confía en valores enviados por el cliente (mismo criterio que la validación de
        // pertenencia del activo al grupo, más arriba).
        var endDate = RentalPeriodCalculator.AddPeriods(request.StartDate, asset.RentalType, request.Periods);
        var totalPrice = RentalPeriodCalculator.CalculateTotalPrice(asset.BasePrice, request.Periods, request.Quantity);

        // El pago se re-verifica server-to-server contra el proveedor correspondiente: jamás se
        // confía en que el frontend haya llegado a este paso porque el pago "fue exitoso" ahí.
        await VerifyPaymentAsync(request, totalPrice, cancellationToken);

        var now = DateTime.UtcNow;
        var customer = await FindOrCreateCustomerAsync(link.LandlordId, request, now, cancellationToken);

        var rental = new Rental
        {
            Id = Guid.NewGuid(),
            AssetId = asset.Id,
            CustomerId = customer.Id,
            StartDate = request.StartDate,
            EndDate = endDate,
            TotalPrice = totalPrice,
            Status = RentalStatus.Active,
            AccessToken = Guid.NewGuid(),
            CreatedAt = now,
            CreatedBy = PortalCreatedBy
        };
        await _rentalRepository.CreateAsync(rental, cancellationToken);

        // El cliente firma en vivo en el paso final de la pasarela, así que el contrato
        // generado para esta renta queda "Firmado" desde el momento cero (a diferencia de
        // una renta creada manualmente en el panel, donde todavía no hay firma a mano).
        var contract = await _contractService.GenerateForRentalAsync(
            rental, asset, customer, link.LandlordId, PortalCreatedBy, request.SignatureImageBase64, cancellationToken);
        rental.ContractPdfUrl = $"/api/contracts/{contract.Id}/pdf";
        rental.UpdatedAt = now;
        rental.UpdatedBy = PortalCreatedBy;
        await _rentalRepository.UpdateAsync(rental, cancellationToken);

        // Igual que en la asignación manual de rentas: el activo pasa a "Rentado".
        asset.Status = AssetStatus.Rented;
        asset.UpdatedAt = now;
        asset.UpdatedBy = PortalCreatedBy;
        await _assetRepository.UpdateAsync(asset, cancellationToken);

        var portalRental = new PortalRental
        {
            Id = Guid.NewGuid(),
            PortalLinkId = link.Id,
            RentalId = rental.Id,
            CustomerId = customer.Id,
            Quantity = request.Quantity,
            Periods = request.Periods,
            PaymentProvider = request.PaymentProvider,
            PaymentReference = request.PaymentReference,
            CreatedAt = now
        };
        await _portalRepository.CreatePortalRentalAsync(portalRental, cancellationToken);

        // Se registra el ingreso en el libro diario (TRX_Payments) con el monto real de negocio
        // (en DOP), no el monto en USD enviado al gateway sandbox (ver nota de moneda en
        // PortalPaymentService).
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            RentalId = rental.Id,
            LandlordId = link.LandlordId,
            Amount = totalPrice,
            Type = PaymentType.Income,
            PaymentDate = now,
            Provider = request.PaymentProvider,
            ExternalReference = request.PaymentReference,
            CreatedAt = now,
            CreatedBy = PortalCreatedBy
        };
        await _paymentRepository.CreateAsync(payment, cancellationToken);

        return new PortalCheckoutResponse(rental.Id, customer.Id, rental.StartDate, rental.EndDate, totalPrice, request.PaymentProvider, request.PaymentReference);
    }

    /// <summary>
    /// Vista previa del contrato ANTES de firmar y pagar (paso 3 de la pasarela, ver
    /// PortalContractPreviewRequest): repite las mismas validaciones tempranas de
    /// CheckoutAsync (link activo, datos del cliente, activo perteneciente al grupo/disponible,
    /// mínimo de períodos) pero SALTEA las de pago/firma, que en este punto todavía no
    /// existen. No persiste nada: solo calcula fecha de fin, precio total y el texto del
    /// contrato mergeado, reutilizando ContractService.PreviewContentAsync.
    /// </summary>
    public async Task<PortalContractPreviewResponse> PreviewContractAsync(string slug, PortalContractPreviewRequest request, CancellationToken cancellationToken = default)
    {
        var link = await GetActiveLinkOrThrowAsync(slug, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new AppValidationException("El nombre es obligatorio.");
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new AppValidationException("El correo es obligatorio.");
        if (request.Quantity < 1)
            throw new AppValidationException("La cantidad debe ser al menos 1.");

        var memberAssetIds = await _assetGroupRepository.GetMemberAssetIdsAsync(link.AssetGroupId, cancellationToken);
        if (!memberAssetIds.Contains(request.AssetId))
            throw new AppValidationException("El activo seleccionado no pertenece a este portal.");

        var asset = await _assetRepository.GetByIdAsync(request.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), request.AssetId);
        if (asset.Status != AssetStatus.Available)
            throw new AppValidationException("El activo seleccionado ya no está disponible.");

        // El mínimo de 1 período es, justamente, la regla de negocio pedida: el cliente no
        // puede rentar menos que la unidad de tiempo configurada en el activo (ej: si el
        // activo es Monthly, no puede rentar menos de 1 mes).
        if (request.Periods < RentalPeriodCalculator.MinimumPeriods)
            throw new AppValidationException(
                $"Para este activo la renta mínima es de {RentalPeriodCalculator.MinimumPeriods} período ({RentalTypeLabel(asset.RentalType)}).");

        // El servidor es la única fuente de verdad para la fecha de fin y el precio: nunca se
        // confía en valores enviados por el cliente (mismo criterio que la validación de
        // pertenencia del activo al grupo, más arriba).
        var endDate = RentalPeriodCalculator.AddPeriods(request.StartDate, asset.RentalType, request.Periods);
        var totalPrice = RentalPeriodCalculator.CalculateTotalPrice(asset.BasePrice, request.Periods, request.Quantity);

        var content = await _contractService.PreviewContentAsync(
            asset, request.FullName, request.IdentityNumber, request.Phone, request.Email, request.Address,
            request.StartDate, endDate, totalPrice, link.LandlordId, cancellationToken);

        return new PortalContractPreviewResponse(content, endDate, totalPrice);
    }

    /// <summary>
    /// Re-verifica el pago contra el proveedor correspondiente ANTES de crear la renta: nunca se
    /// confía en el estado que reporta el frontend. Usa el mismo criterio de conversión de moneda
    /// (DOP -> mismo valor numérico rotulado "USD") que <see cref="PortalPaymentService"/> al
    /// iniciar el cobro, para poder comparar montos.
    /// </summary>
    private async Task VerifyPaymentAsync(PortalCheckoutRequest request, decimal totalPrice, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PaymentReference))
            throw new AppValidationException("El pago no pudo verificarse. Intenta nuevamente.");

        switch (request.PaymentProvider)
        {
            case "Stripe":
            {
                var intent = await _stripeGateway.GetPaymentIntentAsync(request.PaymentReference, cancellationToken);
                var expectedAmountInCents = PortalPaymentService.ToGatewayAmountInCents(totalPrice);
                if (intent is null
                    || !string.Equals(intent.Status, "succeeded", StringComparison.OrdinalIgnoreCase)
                    || intent.AmountInCents != expectedAmountInCents)
                    throw new AppValidationException("El pago no pudo verificarse. Intenta nuevamente.");
                break;
            }
            default:
                throw new AppValidationException("Proveedor de pago no soportado.");
        }
    }

    /// <summary>Etiqueta legible en español de la unidad de tiempo del activo, para mensajes de validación.</summary>
    private static string RentalTypeLabel(RentalTypeOption rentalType) => rentalType switch
    {
        RentalTypeOption.Daily => "día",
        RentalTypeOption.Weekly => "semana",
        RentalTypeOption.Biweekly => "quincena",
        RentalTypeOption.Monthly => "mes",
        RentalTypeOption.Annual => "año",
        _ => rentalType.ToString()
    };

    private async Task<PortalLink> GetActiveLinkOrThrowAsync(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new NotFoundException(nameof(PortalLink), slug);

        return await _portalRepository.GetActiveLinkBySlugAsync(slug.Trim(), cancellationToken)
            ?? throw new NotFoundException(nameof(PortalLink), slug);
    }

    /// <summary>Matchea por Email dentro del negocio del link; si no existe, se crea al vuelo (igual que en la importación masiva de rentas).</summary>
    private async Task<Customer> FindOrCreateCustomerAsync(Guid landlordId, PortalCheckoutRequest request, DateTime now, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var existingCustomers = await _customerRepository.GetAllByLandlordAsync(landlordId, cancellationToken);
        var customer = existingCustomers.FirstOrDefault(c =>
            !string.IsNullOrWhiteSpace(c.Email) && string.Equals(c.Email.Trim(), email, StringComparison.OrdinalIgnoreCase));

        if (customer is not null)
            return customer;

        customer = new Customer
        {
            Id = Guid.NewGuid(),
            LandlordId = landlordId,
            FullName = request.FullName.Trim(),
            IdentityNumber = null,
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            Email = email,
            Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            Country = string.IsNullOrWhiteSpace(request.Country) ? null : request.Country.Trim(),
            CreatedAt = now,
            CreatedBy = PortalCreatedBy
        };
        await _customerRepository.CreateAsync(customer, cancellationToken);
        return customer;
    }

}
