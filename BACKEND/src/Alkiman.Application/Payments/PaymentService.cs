using Alkiman.Application.Assets;
using Alkiman.Application.Common.Exceptions;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Rentals;
using Alkiman.Domain.Entities;

namespace Alkiman.Application.Payments;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _repository;
    private readonly IRentalRepository _rentalRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly ICurrentLandlordService _currentLandlord;

    public PaymentService(
        IPaymentRepository repository,
        IRentalRepository rentalRepository,
        IAssetRepository assetRepository,
        ICurrentLandlordService currentLandlord)
    {
        _repository = repository;
        _rentalRepository = rentalRepository;
        _assetRepository = assetRepository;
        _currentLandlord = currentLandlord;
    }

    public async Task<IReadOnlyList<PaymentResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var payments = await _repository.GetAllByLandlordAsync(landlordId, cancellationToken);
        return payments.Select(ToResponse).ToList();
    }

    public async Task<PaymentResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var payment = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Payment), id);

        if (payment.LandlordId != landlordId)
            throw new ForbiddenException("El movimiento no pertenece al negocio autenticado.");

        return ToResponse(payment);
    }

    public async Task<PaymentResponse> CreateAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Amount < 0)
            throw new AppValidationException("El importe no puede ser negativo.");

        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);

        if (request.RentalId is { } rentalId)
        {
            var rental = await _rentalRepository.GetByIdAsync(rentalId, cancellationToken)
                ?? throw new NotFoundException(nameof(Rental), rentalId);
            var asset = await _assetRepository.GetByIdAsync(rental.AssetId, cancellationToken);
            if (asset is null || asset.LandlordId != landlordId)
                throw new ForbiddenException("La renta indicada no pertenece al negocio autenticado.");
        }

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            RentalId = request.RentalId,
            LandlordId = landlordId,
            Amount = request.Amount,
            Type = request.Type,
            PaymentDate = request.PaymentDate,
            StripeTransactionId = request.StripeTransactionId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentLandlord.Auth0UserId
        };

        await _repository.CreateAsync(payment, cancellationToken);
        return ToResponse(payment);
    }

    private static PaymentResponse ToResponse(Payment payment) => new(
        payment.Id, payment.RentalId, payment.Amount, payment.Type,
        payment.PaymentDate, payment.StripeTransactionId, payment.CreatedAt);
}
