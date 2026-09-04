namespace Alkiman.Application.ContractTemplates;

public interface IContractTemplateService
{
    Task<IReadOnlyList<ContractTemplateResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ContractTemplateResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ContractTemplateResponse> CreateAsync(CreateContractTemplateRequest request, CancellationToken cancellationToken = default);
    Task<ContractTemplateResponse> UpdateAsync(int id, UpdateContractTemplateRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    /// <summary>Marca esta plantilla como la activa de su categoría, desactivando cualquier otra activa de la misma categoría.</summary>
    Task<ContractTemplateResponse> ActivateAsync(int id, CancellationToken cancellationToken = default);
}
