using Alkiman.Domain.Entities;

namespace Alkiman.Application.WhatsApp;

public interface ILandlordWhatsAppRepository
{
    Task<LandlordWhatsApp?> GetByLandlordIdAsync(Guid landlordId, CancellationToken ct = default);
    Task UpsertAsync(LandlordWhatsApp config, CancellationToken ct = default);
}
