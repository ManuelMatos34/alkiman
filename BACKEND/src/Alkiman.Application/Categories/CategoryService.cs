using Alkiman.Application.AuditLogs;
using Alkiman.Application.Common.Exceptions;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Domain.Entities;
using Alkiman.Domain.Enums;

namespace Alkiman.Application.Categories;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repository;
    private readonly ICurrentLandlordService _currentLandlord;
    private readonly IAuditLogService _auditLog;

    public CategoryService(ICategoryRepository repository, ICurrentLandlordService currentLandlord, IAuditLogService auditLog)
    {
        _repository = repository;
        _currentLandlord = currentLandlord;
        _auditLog = auditLog;
    }

    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var categories = await _repository.GetAllByLandlordAsync(landlordId, cancellationToken);
        return categories.Select(ToResponse).ToList();
    }

    public async Task<CategoryResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await GetOwnedOrThrowAsync(id, cancellationToken);
        return ToResponse(category);
    }

    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);

        var category = new Category
        {
            LandlordId = landlordId,
            Name = request.Name,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentLandlord.UserId
        };

        category.Id = await _repository.CreateAsync(category, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Create, "CFG_Categories", category.Id.ToString(), null, category, cancellationToken);
        return ToResponse(category);
    }

    public async Task<CategoryResponse> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await GetOwnedOrThrowAsync(id, cancellationToken);

        category.Name = request.Name;
        category.UpdatedAt = DateTime.UtcNow;
        category.UpdatedBy = _currentLandlord.UserId;

        await _repository.UpdateAsync(category, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Update, "CFG_Categories", category.Id.ToString(), null, category, cancellationToken);
        return ToResponse(category);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await GetOwnedOrThrowAsync(id, cancellationToken);
        await _repository.DeleteAsync(id, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Delete, "CFG_Categories", category.Id.ToString(), category, null, cancellationToken);
    }

    private async Task<Category> GetOwnedOrThrowAsync(int id, CancellationToken cancellationToken)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var category = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), id);

        if (category.LandlordId != landlordId)
            throw new ForbiddenException("La categoría no pertenece al negocio autenticado.");

        return category;
    }

    private static CategoryResponse ToResponse(Category category) =>
        new(category.Id, category.Name, category.CreatedAt);
}
