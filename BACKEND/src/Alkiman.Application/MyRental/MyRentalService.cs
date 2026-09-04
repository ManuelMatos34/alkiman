using Alkiman.Application.Assets;
using Alkiman.Application.Common.Exceptions;
using Alkiman.Application.Customers;
using Alkiman.Application.Landlords;
using Alkiman.Application.RentalRequests;
using Alkiman.Application.Rentals;
using Alkiman.Domain.Common;
using Alkiman.Domain.Entities;
using Alkiman.Domain.Enums;

namespace Alkiman.Application.MyRental;

/// <summary>
/// Flujos públicos (sin autenticación) del link "mi-renta": el cliente entra con el
/// AccessToken de su propia renta (/mi-renta/{token}), verifica su identidad (documento o
/// correo, lo que tenga cargado) y, si está todo en orden, puede pedir una prórroga (solo
/// rentas de largo plazo) o una cancelación anticipada. Igual que <see cref="Alkiman.Application.Portal.PortalService"/>,
/// no hay ICurrentLandlordService acá: el LandlordId de cada operación se deriva del activo
/// de la renta (asset.LandlordId), nunca de un claim JWT, porque no hay usuario logueado.
/// </summary>
public class MyRentalService : IMyRentalService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IRentalRepository _rentalRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IRentalRequestRepository _rentalRequestRepository;
    private readonly ILandlordRepository _landlordRepository;

    public MyRentalService(
        IRentalRepository rentalRepository,
        IAssetRepository assetRepository,
        ICustomerRepository customerRepository,
        IRentalRequestRepository rentalRequestRepository,
        ILandlordRepository landlordRepository)
    {
        _rentalRepository = rentalRepository;
        _assetRepository = assetRepository;
        _customerRepository = customerRepository;
        _rentalRequestRepository = rentalRequestRepository;
        _landlordRepository = landlordRepository;
    }

    public async Task<MyRentalResponse> VerifyAsync(Guid token, VerifyMyRentalRequest request, CancellationToken cancellationToken = default)
    {
        var (rental, asset, _) = await VerifyInternalAsync(token, request.Identifier, cancellationToken);
        return await BuildResponseAsync(rental, asset, cancellationToken);
    }

    public async Task RequestExtensionAsync(Guid token, CreateMyRentalExtensionRequest request, CancellationToken cancellationToken = default)
    {
        var (rental, asset, customer) = await VerifyInternalAsync(token, request.Identifier, cancellationToken);

        if (rental.Status != RentalStatus.Active)
            throw new AppValidationException("Solo se puede pedir una prórroga sobre una renta activa.");

        if (asset.RentalType is not (RentalTypeOption.Monthly or RentalTypeOption.Annual))
            throw new AppValidationException("Las prórrogas solo están disponibles para rentas de largo plazo (mensual o anual).");

        if (request.RequestedPeriods < 1)
            throw new AppValidationException("La cantidad de períodos debe ser al menos 1.");

        if (await _rentalRequestRepository.GetPendingByRentalAsync(rental.Id, cancellationToken) is not null)
            throw new AppValidationException("Ya hay un pedido pendiente para esta renta.");

        var proposedEndDate = RentalPeriodCalculator.AddPeriods(rental.EndDate, asset.RentalType, request.RequestedPeriods);

        var rentalRequest = new RentalRequest
        {
            Id = Guid.NewGuid(),
            LandlordId = asset.LandlordId,
            RentalId = rental.Id,
            Type = RentalRequestType.Extension,
            Status = RentalRequestStatus.Pending,
            RequestedPeriods = request.RequestedPeriods,
            ProposedEndDate = proposedEndDate,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = $"cliente:{customer.FullName}"
        };
        await _rentalRequestRepository.CreateAsync(rentalRequest, cancellationToken);
    }

    public async Task RequestCancellationAsync(Guid token, CreateMyRentalCancellationRequest request, CancellationToken cancellationToken = default)
    {
        var (rental, asset, customer) = await VerifyInternalAsync(token, request.Identifier, cancellationToken);

        if (rental.Status != RentalStatus.Active)
            throw new AppValidationException("Solo se puede cancelar una renta activa.");

        if (await _rentalRequestRepository.GetPendingByRentalAsync(rental.Id, cancellationToken) is not null)
            throw new AppValidationException("Ya hay un pedido pendiente para esta renta.");

        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new AppValidationException("Contanos el motivo de la cancelación.");

        var rentalRequest = new RentalRequest
        {
            Id = Guid.NewGuid(),
            LandlordId = asset.LandlordId,
            RentalId = rental.Id,
            Type = RentalRequestType.Cancellation,
            Status = RentalRequestStatus.Pending,
            Reason = request.Reason.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = $"cliente:{customer.FullName}"
        };
        await _rentalRequestRepository.CreateAsync(rentalRequest, cancellationToken);
    }

    private async Task<MyRentalResponse> BuildResponseAsync(Rental rental, Asset asset, CancellationToken cancellationToken)
    {
        var pending = await _rentalRequestRepository.GetPendingByRentalAsync(rental.Id, cancellationToken);
        var landlord = await _landlordRepository.GetByIdAsync(asset.LandlordId, cancellationToken);

        var isLongTerm = asset.RentalType is RentalTypeOption.Monthly or RentalTypeOption.Annual;
        var isActive = rental.Status == RentalStatus.Active;

        return new MyRentalResponse(
            rental.Id,
            asset.Name,
            asset.Description,
            rental.StartDate,
            rental.EndDate,
            rental.TotalPrice,
            rental.Status.ToString(),
            rental.ContractPdfUrl,
            CanRequestExtension: isActive && isLongTerm && pending is null,
            CanRequestCancellation: isActive && pending is null,
            PendingRequest: pending is null ? null : new RentalRequestSummary(pending.Id, pending.Type.ToString(), pending.CreatedAt),
            AppName: landlord?.AppName ?? "Alkiman",
            ThemeMode: landlord?.ThemeMode ?? "light",
            AccentColor: landlord?.AccentColor ?? "blue");
    }

    /// <summary>
    /// Verifica el AccessToken + la identidad provista (con lockout simple contra intentos de
    /// adivinar) y devuelve la renta/activo/cliente ya resueltos, listos para usar en el resto
    /// de las operaciones públicas.
    /// </summary>
    private async Task<(Rental Rental, Asset Asset, Customer Customer)> VerifyInternalAsync(Guid token, string identifier, CancellationToken cancellationToken)
    {
        var rental = await _rentalRepository.GetByAccessTokenAsync(token, cancellationToken)
            ?? throw new NotFoundException(nameof(Rental), token);

        if (rental.AccessLockedUntil is { } lockedUntil && lockedUntil > DateTime.UtcNow)
            throw new AppValidationException("Demasiados intentos. Inténtalo de nuevo más tarde.");

        var customer = await _customerRepository.GetByIdAsync(rental.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), rental.CustomerId);

        if (!MatchesIdentity(customer, identifier))
        {
            rental.AccessFailedAttempts++;
            if (rental.AccessFailedAttempts >= MaxFailedAttempts)
            {
                rental.AccessLockedUntil = DateTime.UtcNow.Add(LockoutDuration);
                rental.AccessFailedAttempts = 0;
            }
            await _rentalRepository.UpdateAccessStateAsync(rental.Id, rental.AccessFailedAttempts, rental.AccessLockedUntil, cancellationToken);
            throw new AppValidationException("Los datos no coinciden con esta renta.");
        }

        await _rentalRepository.UpdateAccessStateAsync(rental.Id, 0, null, cancellationToken);

        var asset = await _assetRepository.GetByIdAsync(rental.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), rental.AssetId);

        return (rental, asset, customer);
    }

    private static string NormalizeIdentity(string? value) =>
        (value ?? string.Empty).Trim().Replace("-", "").Replace(" ", "").ToUpperInvariant();

    /// <summary>
    /// Matchea contra documento O correo, lo que el cliente tenga cargado: los clientes que
    /// vienen del Portal público nunca tienen IdentityNumber (ver Customer.cs y
    /// PortalService.FindOrCreateCustomerAsync, que siempre lo deja en null), así que el correo
    /// es lo único contra lo que se los puede verificar. Los clientes dados de alta manualmente
    /// desde el panel suelen tener ambos.
    /// </summary>
    private static bool MatchesIdentity(Customer customer, string providedIdentifier)
    {
        var normalizedProvided = NormalizeIdentity(providedIdentifier);

        if (!string.IsNullOrWhiteSpace(customer.IdentityNumber) && NormalizeIdentity(customer.IdentityNumber) == normalizedProvided)
            return true;

        if (!string.IsNullOrWhiteSpace(customer.Email) && customer.Email.Trim().Equals(providedIdentifier.Trim(), StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }
}
