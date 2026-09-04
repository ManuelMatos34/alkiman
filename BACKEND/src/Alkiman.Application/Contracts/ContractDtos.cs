using Alkiman.Domain.Enums;

namespace Alkiman.Application.Contracts;

public record ContractResponse(
    Guid Id,
    Guid RentalId,
    Guid CustomerId,
    string CustomerName,
    int? ContractTemplateId,
    string? TemplateName,
    ContractStatus Status,
    DateTime? SignedAt,
    bool EmailSent,
    DateTime CreatedAt);

/// <summary>Firma capturada como imagen PNG en Base64 (sin el prefijo "data:image/png;base64,").</summary>
public record SignContractRequest(string SignatureImageBase64);
