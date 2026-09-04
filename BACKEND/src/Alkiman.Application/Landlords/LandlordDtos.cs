namespace Alkiman.Application.Landlords;

public record LandlordResponse(
    Guid Id,
    string BusinessName,
    string AppName,
    string ThemeMode,
    string AccentColor,
    int? CountryId,
    int? StateId,
    int? CityId,
    string? Address,
    string? Phone1,
    string? Phone2,
    string? TaxId,
    DateTime CreatedAt,
    string? SignatureBase64);

public record UpdateLandlordRequest(
    string BusinessName,
    int? CountryId,
    int? StateId,
    int? CityId,
    string? Address,
    string? Phone1,
    string? Phone2,
    string? TaxId);

public record UpdateAppearanceRequest(string AppName, string ThemeMode, string AccentColor);

public record UpdateLandlordSignatureRequest(string? SignatureBase64);
