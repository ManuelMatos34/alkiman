namespace Alkiman.Application.Emails;

public record EmailMessageResponse(
    Guid Id,
    string Type,
    Guid? CustomerId,
    string RecipientName,
    string RecipientEmail,
    string Subject,
    string Body,
    string Status,
    string? ErrorMessage,
    DateTime? SentAt,
    DateTime CreatedAt
);

public record SendIndividualEmailRequest(
    Guid? CustomerId,
    string RecipientName,
    string RecipientEmail,
    string Subject,
    string Body
);

public record SendMassEmailRequest(
    IReadOnlyList<Guid> CustomerIds,
    string Subject,
    string Body
);

public record SendMassEmailResponse(
    int Sent,
    int Failed,
    IReadOnlyList<EmailMessageResponse> Messages
);
