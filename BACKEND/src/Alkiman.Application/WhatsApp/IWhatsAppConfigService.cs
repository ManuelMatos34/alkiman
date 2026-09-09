namespace Alkiman.Application.WhatsApp;

public interface IWhatsAppConfigService
{
    Task<WhatsAppConfigResponse> GetConfigAsync(CancellationToken ct = default);
    Task<WhatsAppConfigResponse> SaveConfigAsync(WhatsAppConfigRequest request, CancellationToken ct = default);
}
