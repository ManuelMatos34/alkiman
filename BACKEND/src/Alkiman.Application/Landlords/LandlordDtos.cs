namespace Alkiman.Application.Landlords;

public record LandlordResponse(Guid Id, string BusinessName, string Email, DateTime CreatedAt);

public record RegisterLandlordRequest(string BusinessName, string Email);

public record UpdateLandlordRequest(string BusinessName, string Email);
