namespace Alkiman.Application.Me;

public record MeResponse(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    bool IsOwner,
    IReadOnlyList<string> Permissions,
    bool TwoFactorEnabled,
    Guid LandlordId,
    string BusinessName,
    DateTime CreatedAt);

public record UpdateMeRequest(string FullName, string Email);

public record ChangeMyPasswordRequest(string CurrentPassword, string NewPassword);

public record UpdateMyTwoFactorRequest(bool Enabled);
