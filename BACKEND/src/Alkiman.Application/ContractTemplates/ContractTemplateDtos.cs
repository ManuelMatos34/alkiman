namespace Alkiman.Application.ContractTemplates;

public record ContractTemplateResponse(
    int Id,
    int CategoryId,
    string CategoryName,
    string Name,
    string Content,
    bool IsActive,
    DateTime CreatedAt);

public record CreateContractTemplateRequest(int CategoryId, string Name, string Content);

public record UpdateContractTemplateRequest(string Name, string Content);
