using Alkiman.Application.Common.Interfaces;

namespace Alkiman.Application.WhatsApp;

public class WhatsAppStatsService : IWhatsAppStatsService
{
    private const int Limit = 1000;
    private const int WarningThreshold = 900;

    private readonly IWhatsAppMessageRepository _repository;
    private readonly ICurrentLandlordService _currentLandlord;

    public WhatsAppStatsService(IWhatsAppMessageRepository repository, ICurrentLandlordService currentLandlord)
    {
        _repository = repository;
        _currentLandlord = currentLandlord;
    }

    public async Task<WhatsAppStatsResponse> GetStatsAsync(CancellationToken ct = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(ct);
        var now = DateTime.UtcNow;
        var count = await _repository.GetMonthlyCountAsync(landlordId, now.Year, now.Month, ct);

        return new WhatsAppStatsResponse(
            SentThisMonth: count,
            Limit: Limit,
            WarningThreshold: WarningThreshold,
            IsWarning: count >= WarningThreshold,
            IsAtLimit: count >= Limit
        );
    }
}
