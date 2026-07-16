using Alkiman.Domain.Enums;

namespace Alkiman.Application.Rentals;

public record RentalResponse(
    Guid Id,
    Guid AssetId,
    Guid CustomerId,
    DateTime StartDate,
    DateTime EndDate,
    string? ContractPdfUrl,
    decimal TotalPrice,
    RentalStatus Status,
    DateTime CreatedAt);

public record CreateRentalRequest(Guid AssetId, Guid CustomerId, DateTime StartDate, DateTime EndDate, decimal TotalPrice);

public record UpdateRentalContractRequest(string ContractPdfUrl);

public record UpdateRentalStatusRequest(RentalStatus Status);
