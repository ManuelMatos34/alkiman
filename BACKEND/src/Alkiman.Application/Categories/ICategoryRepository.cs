using Alkiman.Domain.Entities;

namespace Alkiman.Application.Categories;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllByLandlordAsync(Guid landlordId, CancellationToken cancellationToken = default);
    Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(Category category, CancellationToken cancellationToken = default);
    Task UpdateAsync(Category category, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
