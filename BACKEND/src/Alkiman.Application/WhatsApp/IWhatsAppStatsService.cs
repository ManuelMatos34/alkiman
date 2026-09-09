namespace Alkiman.Application.WhatsApp;

public interface IWhatsAppStatsService
{
    Task<WhatsAppStatsResponse> GetStatsAsync(CancellationToken ct = default);
}
