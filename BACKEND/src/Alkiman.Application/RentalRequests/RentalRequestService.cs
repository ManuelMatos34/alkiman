using Alkiman.Application.Assets;
using Alkiman.Application.Common.Exceptions;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Customers;
using Alkiman.Application.Rentals;
using Alkiman.Application.WhatsApp;
using Alkiman.Domain.Common;
using Alkiman.Domain.Entities;
using Alkiman.Domain.Enums;

namespace Alkiman.Application.RentalRequests;

/// <summary>
/// Revisión (panel autenticado) de los pedidos de prórroga/cancelación que el cliente hace
/// desde su link público "mi-renta" (ver <see cref="Alkiman.Application.MyRental.MyRentalService"/>,
/// que es quien los crea). Aprobar/rechazar nunca dispara un cobro: los pagos siguen siendo
/// 100% manuales, igual que en el resto de la aplicación.
/// </summary>
public class RentalRequestService : IRentalRequestService
{
    private readonly IRentalRequestRepository _repository;
    private readonly IRentalRepository _rentalRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IWhatsAppSender _whatsAppSender;
    private readonly ICurrentLandlordService _currentLandlord;

    public RentalRequestService(
        IRentalRequestRepository repository,
        IRentalRepository rentalRepository,
        IAssetRepository assetRepository,
        ICustomerRepository customerRepository,
        IWhatsAppSender whatsAppSender,
        ICurrentLandlordService currentLandlord)
    {
        _repository = repository;
        _rentalRepository = rentalRepository;
        _assetRepository = assetRepository;
        _customerRepository = customerRepository;
        _whatsAppSender = whatsAppSender;
        _currentLandlord = currentLandlord;
    }

    public async Task<IReadOnlyList<RentalRequestResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var requests = await _repository.GetAllByLandlordAsync(landlordId, cancellationToken);

        var responses = new List<RentalRequestResponse>(requests.Count);
        foreach (var request in requests)
        {
            var rental = await _rentalRepository.GetByIdAsync(request.RentalId, cancellationToken);
            if (rental is null)
                continue;

            var asset = await _assetRepository.GetByIdAsync(rental.AssetId, cancellationToken);
            var customer = await _customerRepository.GetByIdAsync(rental.CustomerId, cancellationToken);
            responses.Add(ToResponse(request, asset, customer));
        }
        return responses;
    }

    public async Task<RentalRequestResponse> ApproveAsync(Guid id, ReviewRentalRequestRequest request, CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var (reqEntity, rental, asset, customer) = await LoadOwnedAsync(id, landlordId, cancellationToken);

        if (reqEntity.Status != RentalRequestStatus.Pending)
            throw new AppValidationException("Este pedido ya fue resuelto.");

        var now = DateTime.UtcNow;

        if (reqEntity.Type == RentalRequestType.Extension)
        {
            if (rental.Status != RentalStatus.Active)
                throw new AppValidationException("Solo se puede extender una renta activa.");

            rental.EndDate = RentalPeriodCalculator.AddPeriods(rental.EndDate, asset.RentalType, reqEntity.RequestedPeriods!.Value);
            rental.UpdatedAt = now;
            rental.UpdatedBy = _currentLandlord.UserId;
            await _rentalRepository.UpdateAsync(rental, cancellationToken);
        }
        else
        {
            if (rental.Status != RentalStatus.Active)
                throw new AppValidationException("Solo se puede cancelar una renta activa.");

            rental.Status = RentalStatus.Cancelled;
            rental.UpdatedAt = now;
            rental.UpdatedBy = _currentLandlord.UserId;
            await _rentalRepository.UpdateAsync(rental, cancellationToken);

            asset.Status = AssetStatus.Available;
            asset.UpdatedAt = now;
            asset.UpdatedBy = _currentLandlord.UserId;
            await _assetRepository.UpdateAsync(asset, cancellationToken);
        }

        reqEntity.Status = RentalRequestStatus.Approved;
        reqEntity.StaffNote = request.StaffNote;
        reqEntity.ReviewedAt = now;
        reqEntity.ReviewedBy = _currentLandlord.UserId;
        reqEntity.UpdatedAt = now;
        reqEntity.UpdatedBy = _currentLandlord.UserId;
        await _repository.UpdateAsync(reqEntity, cancellationToken);

        await TryNotifyCustomerAsync(landlordId, customer, reqEntity, asset, approved: true, cancellationToken);

        return ToResponse(reqEntity, asset, customer);
    }

    public async Task<RentalRequestResponse> RejectAsync(Guid id, ReviewRentalRequestRequest request, CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var (reqEntity, _, asset, customer) = await LoadOwnedAsync(id, landlordId, cancellationToken);

        if (reqEntity.Status != RentalRequestStatus.Pending)
            throw new AppValidationException("Este pedido ya fue resuelto.");

        var now = DateTime.UtcNow;
        reqEntity.Status = RentalRequestStatus.Rejected;
        reqEntity.StaffNote = request.StaffNote;
        reqEntity.ReviewedAt = now;
        reqEntity.ReviewedBy = _currentLandlord.UserId;
        reqEntity.UpdatedAt = now;
        reqEntity.UpdatedBy = _currentLandlord.UserId;
        await _repository.UpdateAsync(reqEntity, cancellationToken);

        await TryNotifyCustomerAsync(landlordId, customer, reqEntity, asset, approved: false, cancellationToken);

        return ToResponse(reqEntity, asset, customer);
    }

    private async Task<(RentalRequest Request, Rental Rental, Asset Asset, Customer Customer)> LoadOwnedAsync(
        Guid requestId, Guid landlordId, CancellationToken cancellationToken)
    {
        var request = await _repository.GetByIdAsync(requestId, cancellationToken)
            ?? throw new NotFoundException(nameof(RentalRequest), requestId);

        if (request.LandlordId != landlordId)
            throw new ForbiddenException("Este pedido no pertenece al negocio autenticado.");

        var rental = await _rentalRepository.GetByIdAsync(request.RentalId, cancellationToken)
            ?? throw new NotFoundException(nameof(Rental), request.RentalId);
        var asset = await _assetRepository.GetByIdAsync(rental.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), rental.AssetId);
        var customer = await _customerRepository.GetByIdAsync(rental.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), rental.CustomerId);

        return (request, rental, asset, customer);
    }

    /// <summary>Notifica al cliente el resultado de su pedido vía WhatsApp. Nunca lanza: no puede bloquear la aprobación/rechazo. Si el cliente no tiene teléfono, no se envía nada.</summary>
    private async Task TryNotifyCustomerAsync(Guid _, Customer customer, RentalRequest request, Asset __, bool approved, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(customer.Phone))
            return;

        var actionLabel = request.Type == RentalRequestType.Extension ? "prórroga" : "cancelación";
        var resultado   = approved ? "aprobado" : "rechazado";

        try
        {
            await _whatsAppSender.SendTemplateAsync(
                customer.Phone,
                WhatsAppTemplates.RentalDecision,
                "es",
                [customer.FullName.Split(' ')[0], actionLabel, resultado],
                cancellationToken);
        }
        catch
        {
            // Best effort: un fallo de notificación nunca debe bloquear la aprobación/rechazo.
        }
    }

    private static RentalRequestResponse ToResponse(RentalRequest request, Asset? asset, Customer? customer) => new(
        request.Id, request.RentalId, asset?.Id ?? Guid.Empty, asset?.Name ?? "—",
        customer?.Id ?? Guid.Empty, customer?.FullName ?? "—",
        request.Type, request.Status, request.RequestedPeriods, request.ProposedEndDate,
        request.Reason, request.StaffNote, request.ReviewedAt, request.ReviewedBy, request.CreatedAt);
}
