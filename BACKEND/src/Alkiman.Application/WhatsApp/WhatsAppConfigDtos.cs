namespace Alkiman.Application.WhatsApp;

public record WhatsAppConfigRequest(
    string PhoneNumberId,
    string AccessToken,
    string? PhoneNumber,
    string? DisplayName
);

public record WhatsAppConfigResponse(
    bool IsConfigured,
    string? PhoneNumber,
    string? DisplayName,
    bool IsActive
);
