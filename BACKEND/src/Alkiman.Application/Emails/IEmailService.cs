namespace Alkiman.Application.Emails;

public interface IEmailService
{
    Task<IReadOnlyList<EmailMessageResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<EmailMessageResponse> SendIndividualAsync(SendIndividualEmailRequest request, CancellationToken cancellationToken = default);
    Task<SendMassEmailResponse> SendMassAsync(SendMassEmailRequest request, CancellationToken cancellationToken = default);
}
