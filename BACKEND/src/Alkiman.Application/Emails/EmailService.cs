using Alkiman.Application.Common.Exceptions;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Customers;
using Alkiman.Domain.Entities;

namespace Alkiman.Application.Emails;

public class EmailService : IEmailService
{
    private readonly IEmailRepository _repository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IEmailSender _sender;
    private readonly ICurrentLandlordService _currentLandlord;

    public EmailService(
        IEmailRepository repository,
        ICustomerRepository customerRepository,
        IEmailSender sender,
        ICurrentLandlordService currentLandlord)
    {
        _repository = repository;
        _customerRepository = customerRepository;
        _sender = sender;
        _currentLandlord = currentLandlord;
    }

    public async Task<IReadOnlyList<EmailMessageResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var messages = await _repository.GetAllByLandlordAsync(landlordId, cancellationToken);
        return messages.Select(ToResponse).ToList();
    }

    public async Task<EmailMessageResponse> SendIndividualAsync(SendIndividualEmailRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RecipientEmail))
            throw new AppValidationException("El correo del destinatario es obligatorio.");
        if (string.IsNullOrWhiteSpace(request.Subject))
            throw new AppValidationException("El asunto es obligatorio.");
        if (string.IsNullOrWhiteSpace(request.Body))
            throw new AppValidationException("El mensaje es obligatorio.");

        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);

        Guid? customerId = null;
        if (request.CustomerId.HasValue)
        {
            var customer = await _customerRepository.GetByIdAsync(request.CustomerId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(Customer), request.CustomerId.Value);
            if (customer.LandlordId != landlordId)
                throw new ForbiddenException("El cliente no pertenece al negocio autenticado.");
            customerId = customer.Id;
        }

        var message = await SendAndPersistAsync(
            landlordId, "Individual", customerId, request.RecipientName, request.RecipientEmail,
            request.Subject, request.Body, cancellationToken);

        return ToResponse(message);
    }

    public async Task<SendMassEmailResponse> SendMassAsync(SendMassEmailRequest request, CancellationToken cancellationToken = default)
    {
        if (request.CustomerIds is not { Count: > 0 })
            throw new AppValidationException("Debe seleccionar al menos un cliente destinatario.");
        if (string.IsNullOrWhiteSpace(request.Subject))
            throw new AppValidationException("El asunto es obligatorio.");
        if (string.IsNullOrWhiteSpace(request.Body))
            throw new AppValidationException("El mensaje es obligatorio.");

        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);

        var messages = new List<EmailMessage>();
        foreach (var customerId in request.CustomerIds.Distinct())
        {
            var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
            if (customer is null || customer.LandlordId != landlordId)
                continue; // Cliente inexistente o de otro negocio: se omite en silencio.

            if (string.IsNullOrWhiteSpace(customer.Email))
            {
                var noEmailMessage = new EmailMessage
                {
                    LandlordId = landlordId,
                    CustomerId = customer.Id,
                    Type = "Mass",
                    RecipientName = customer.FullName,
                    RecipientEmail = string.Empty,
                    Subject = request.Subject,
                    Body = request.Body,
                    Status = "Failed",
                    ErrorMessage = "El cliente no tiene un correo registrado.",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = _currentLandlord.UserId
                };
                noEmailMessage.Id = await _repository.CreateAsync(noEmailMessage, cancellationToken);
                messages.Add(noEmailMessage);
                continue;
            }

            var message = await SendAndPersistAsync(
                landlordId, "Mass", customer.Id, customer.FullName, customer.Email!,
                request.Subject, request.Body, cancellationToken);
            messages.Add(message);
        }

        var sent = messages.Count(m => m.Status == "Sent");
        return new SendMassEmailResponse(sent, messages.Count - sent, messages.Select(ToResponse).ToList());
    }

    private async Task<EmailMessage> SendAndPersistAsync(
        Guid landlordId, string type, Guid? customerId, string recipientName, string recipientEmail,
        string subject, string body, CancellationToken cancellationToken)
    {
        var result = await _sender.SendAsync(recipientEmail, recipientName, subject, body, cancellationToken);

        var message = new EmailMessage
        {
            LandlordId = landlordId,
            CustomerId = customerId,
            Type = type,
            RecipientName = recipientName,
            RecipientEmail = recipientEmail,
            Subject = subject,
            Body = body,
            Status = result.Success ? "Sent" : "Failed",
            ErrorMessage = result.Success ? null : result.ErrorMessage,
            SentAt = result.Success ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentLandlord.UserId
        };

        message.Id = await _repository.CreateAsync(message, cancellationToken);
        return message;
    }

    private static EmailMessageResponse ToResponse(EmailMessage message) => new(
        message.Id,
        message.Type,
        message.CustomerId,
        message.RecipientName,
        message.RecipientEmail,
        message.Subject,
        message.Body,
        message.Status,
        message.ErrorMessage,
        message.SentAt,
        message.CreatedAt
    );
}
