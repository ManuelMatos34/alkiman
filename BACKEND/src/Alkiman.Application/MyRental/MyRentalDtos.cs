namespace Alkiman.Application.MyRental;

public record VerifyMyRentalRequest(string Identifier);

public record RentalRequestSummary(Guid Id, string Type, DateTime CreatedAt);

public record MyRentalResponse(
    Guid RentalId,
    string AssetName,
    string? AssetDescription,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalPrice,
    string Status,
    string? ContractPdfUrl,
    bool CanRequestExtension,
    bool CanRequestCancellation,
    RentalRequestSummary? PendingRequest,
    string AppName,
    string ThemeMode,
    string AccentColor);

public record CreateMyRentalExtensionRequest(string Identifier, int RequestedPeriods);

public record CreateMyRentalCancellationRequest(string Identifier, string Reason);
