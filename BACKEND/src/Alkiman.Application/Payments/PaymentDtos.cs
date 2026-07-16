using Alkiman.Domain.Enums;

namespace Alkiman.Application.Payments;

public record PaymentResponse(
    Guid Id,
    Guid? RentalId,
    decimal Amount,
    PaymentType Type,
    DateTime PaymentDate,
    string? StripeTransactionId,
    DateTime CreatedAt);

public record CreatePaymentRequest(Guid? RentalId, decimal Amount, PaymentType Type, DateTime PaymentDate, string? StripeTransactionId);
