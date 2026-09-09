namespace Alkiman.Application.WhatsApp;

public record WhatsAppStatsResponse(
    int SentThisMonth,
    int Limit,
    int WarningThreshold,
    bool IsWarning,
    bool IsAtLimit
);
