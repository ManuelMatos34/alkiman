using Alkiman.Domain.Entities;

namespace Alkiman.Application.ContractTemplates;

public interface IContractTemplateRepository
{
    Task<IReadOnlyList<ContractTemplate>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default);
    Task<ContractTemplate?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    /// <summary>Plantilla activa de una categoría (la que se usa para nuevas rentas), si existe alguna.</summary>
    Task<ContractTemplate?> GetActiveByCategoryAsync(Guid landlordId, int categoryId, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(ContractTemplate template, CancellationToken cancellationToken = default);
    Task UpdateAsync(ContractTemplate template, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    /// <summary>Desactiva todas las plantillas de la categoría (se usa antes de activar una nueva, para que solo haya una activa a la vez).</summary>
    Task DeactivateAllForCategoryAsync(Guid landlordId, int categoryId, CancellationToken cancellationToken = default);
}
