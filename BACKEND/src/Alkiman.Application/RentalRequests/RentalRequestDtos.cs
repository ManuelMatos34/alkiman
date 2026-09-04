using Alkiman.Domain.Enums;

namespace Alkiman.Application.RentalRequests;

public record RentalRequestResponse(
    Guid Id,
    Guid RentalId,
    Guid AssetId,
    string AssetName,
    Guid CustomerId,
    string CustomerName,
    RentalRequestType Type,
    RentalRequestStatus Status,
    int? RequestedPeriods,
    DateTime? ProposedEndDate,
    string? Reason,
    string? StaffNote,
    DateTime? ReviewedAt,
    string? ReviewedBy,
    DateTime CreatedAt);

public record ReviewRentalRequestRequest(string? StaffNote);
