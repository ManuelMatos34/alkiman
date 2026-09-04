using Alkiman.Domain.Entities;

namespace Alkiman.Application.Emails;

public interface IEmailRepository
{
    Task<IReadOnlyList<EmailMessage>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
