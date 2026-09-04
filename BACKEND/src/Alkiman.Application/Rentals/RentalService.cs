using Alkiman.Application.Assets;
using Alkiman.Application.AuditLogs;
using Alkiman.Application.Common.Exceptions;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Contracts;
using Alkiman.Application.Customers;
using Alkiman.Domain.Entities;
using Alkiman.Domain.Enums;

namespace Alkiman.Application.Rentals;

public class RentalService : IRentalService
{
    private readonly IRentalRepository _repository;
    private readonly IAssetRepository _assetRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IContractService _contractService;
    private readonly ICurrentLandlordService _currentLandlord;
    private readonly IAuditLogService _auditLog;

    public RentalService(
        IRentalRepository repository,
        IAssetRepository assetRepository,
        ICustomerRepository customerRepository,
        IContractService contractService,
        ICurrentLandlordService currentLandlord,
        IAuditLogService auditLog)
    {
        _repository = repository;
        _assetRepository = assetRepository;
        _customerRepository = customerRepository;
        _contractService = contractService;
        _currentLandlord = currentLandlord;
        _auditLog = auditLog;
    }

    public async Task<IReadOnlyList<RentalResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var rentals = await _repository.GetAllByLandlordAsync(landlordId, cancellationToken);
        return rentals.Select(ToResponse).ToList();
    }

    public async Task<RentalResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rental = await GetOwnedOrThrowAsync(id, cancellationToken);
        return ToResponse(rental);
    }

    public async Task<RentalResponse> CreateAsync(CreateRentalRequest request, CancellationToken cancellationToken = default)
    {
        if (request.EndDate <= request.StartDate)
            throw new AppValidationException("La fecha de fin debe ser posterior a la fecha de inicio.");
        if (request.TotalPrice < 0)
            throw new AppValidationException("El precio total no puede ser negativo.");

        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);

        var asset = await _assetRepository.GetByIdAsync(request.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), request.AssetId);
        if (asset.LandlordId != landlordId)
            throw new ForbiddenException("El activo no pertenece al negocio autenticado.");

        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.CustomerId);
        if (customer.LandlordId != landlordId)
            throw new ForbiddenException("El cliente no pertenece al negocio autenticado.");

        var rental = new Rental
        {
            Id = Guid.NewGuid(),
            AssetId = request.AssetId,
            CustomerId = request.CustomerId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalPrice = request.TotalPrice,
            Status = RentalStatus.Active,
            AccessToken = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentLandlord.UserId
        };

        await _repository.CreateAsync(rental, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Create, "TRX_Rentals", rental.Id.ToString(), null, rental, cancellationToken);

        // Cada renta que se crea debe generar su contrato automáticamente (con el detalle
        // de la renta) y enviárselo al cliente por correo. Al crearse manualmente desde el
        // panel todavía no hay firma del cliente a mano: el contrato queda "Pendiente" y se
        // puede firmar luego desde el mantenimiento de Contratos.
        var contract = await _contractService.GenerateForRentalAsync(
            rental, asset, customer, landlordId, _currentLandlord.UserId, signatureImageBase64: null, cancellationToken);
        rental.ContractPdfUrl = $"/api/contracts/{contract.Id}/pdf";
        rental.UpdatedAt = DateTime.UtcNow;
        rental.UpdatedBy = _currentLandlord.UserId;
        await _repository.UpdateAsync(rental, cancellationToken);

        // Happy path: al asignar la renta, el activo pasa automáticamente a "Rentado".
        asset.Status = AssetStatus.Rented;
        asset.UpdatedAt = DateTime.UtcNow;
        asset.UpdatedBy = _currentLandlord.UserId;
        await _assetRepository.UpdateAsync(asset, cancellationToken);

        return ToResponse(rental);
    }

    public async Task<RentalResponse> AttachContractAsync(Guid id, UpdateRentalContractRequest request, CancellationToken cancellationToken = default)
    {
        var rental = await GetOwnedOrThrowAsync(id, cancellationToken);

        rental.ContractPdfUrl = request.ContractPdfUrl;
        rental.UpdatedAt = DateTime.UtcNow;
        rental.UpdatedBy = _currentLandlord.UserId;

        await _repository.UpdateAsync(rental, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Update, "TRX_Rentals", rental.Id.ToString(), null, rental, cancellationToken);
        return ToResponse(rental);
    }

    public async Task<RentalResponse> UpdateStatusAsync(Guid id, UpdateRentalStatusRequest request, CancellationToken cancellationToken = default)
    {
        var rental = await GetOwnedOrThrowAsync(id, cancellationToken);

        rental.Status = request.Status;
        rental.UpdatedAt = DateTime.UtcNow;
        rental.UpdatedBy = _currentLandlord.UserId;

        await _repository.UpdateAsync(rental, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Update, "TRX_Rentals", rental.Id.ToString(), null, rental, cancellationToken);

        if (request.Status == RentalStatus.Completed)
        {
            var asset = await _assetRepository.GetByIdAsync(rental.AssetId, cancellationToken);
            if (asset is not null)
            {
                asset.Status = AssetStatus.Available;
                asset.UpdatedAt = DateTime.UtcNow;
                asset.UpdatedBy = _currentLandlord.UserId;
                await _assetRepository.UpdateAsync(asset, cancellationToken);
            }
        }

        return ToResponse(rental);
    }

    private async Task<Rental> GetOwnedOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var rental = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Rental), id);

        var asset = await _assetRepository.GetByIdAsync(rental.AssetId, cancellationToken);
        if (asset is null || asset.LandlordId != landlordId)
            throw new ForbiddenException("La renta no pertenece al negocio autenticado.");

        return rental;
    }

    private static RentalResponse ToResponse(Rental rental) => new(
        rental.Id, rental.AssetId, rental.CustomerId, rental.StartDate, rental.EndDate,
        rental.ContractPdfUrl, rental.TotalPrice, rental.Status, rental.AccessToken, rental.CreatedAt);
}
