using Alkiman.Application.Common.Interfaces;
using Alkiman.Domain.Entities;

namespace Alkiman.Application.WhatsApp;

public class WhatsAppConfigService : IWhatsAppConfigService
{
    private readonly ILandlordWhatsAppRepository _repo;
    private readonly ICurrentLandlordService _currentLandlord;

    public WhatsAppConfigService(ILandlordWhatsAppRepository repo, ICurrentLandlordService currentLandlord)
    {
        _repo = repo;
        _currentLandlord = currentLandlord;
    }

    public async Task<WhatsAppConfigResponse> GetConfigAsync(CancellationToken ct = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(ct);
        var config = await _repo.GetByLandlordIdAsync(landlordId, ct);

        if (config is null)
            return new WhatsAppConfigResponse(false, null, null, false);

        return new WhatsAppConfigResponse(
            IsConfigured: true,
            PhoneNumber: config.PhoneNumber,
            DisplayName: config.DisplayName,
            IsActive: config.IsActive
        );
    }

    public async Task<WhatsAppConfigResponse> SaveConfigAsync(WhatsAppConfigRequest request, CancellationToken ct = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(ct);
        var existing = await _repo.GetByLandlordIdAsync(landlordId, ct);

        var config = existing ?? new LandlordWhatsApp
        {
            Id = Guid.NewGuid(),
            LandlordId = landlordId,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
        };

        config.PhoneNumberId = request.PhoneNumberId;
        config.AccessToken = request.AccessToken;
        config.PhoneNumber = request.PhoneNumber;
        config.DisplayName = request.DisplayName;
        config.UpdatedAt = DateTime.UtcNow;

        await _repo.UpsertAsync(config, ct);

        return new WhatsAppConfigResponse(
            IsConfigured: true,
            PhoneNumber: config.PhoneNumber,
            DisplayName: config.DisplayName,
            IsActive: config.IsActive
        );
    }
}
