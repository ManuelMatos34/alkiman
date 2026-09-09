using Alkiman.Domain.Entities;

namespace Alkiman.Application.WhatsApp;

public interface IWhatsAppMessageRepository
{
    Task CreateAsync(WhatsAppMessage message, CancellationToken ct = default);
    Task<int> GetMonthlyCountAsync(Guid landlordId, int year, int month, CancellationToken ct = default);
}
