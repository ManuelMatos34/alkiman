using Alkiman.Application.AuditLogs;
using Alkiman.Application.Categories;
using Alkiman.Application.Common.Exceptions;
using Alkiman.Application.Common.Interfaces;
using Alkiman.Domain.Entities;
using Alkiman.Domain.Enums;

namespace Alkiman.Application.ContractTemplates;

/// <summary>
/// Mantenimiento de plantillas de contrato. Un negocio puede tener varias plantillas
/// por Categoría (historial de versiones), pero solo una activa a la vez por
/// categoría: esa es la que ContractService usa para generar el contrato cuando se
/// crea una renta de un activo de esa categoría (ver GetActiveByCategoryAsync).
/// </summary>
public class ContractTemplateService : IContractTemplateService
{
    private readonly IContractTemplateRepository _repository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICurrentLandlordService _currentLandlord;
    private readonly IAuditLogService _auditLog;

    public ContractTemplateService(
        IContractTemplateRepository repository,
        ICategoryRepository categoryRepository,
        ICurrentLandlordService currentLandlord,
        IAuditLogService auditLog)
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
        _currentLandlord = currentLandlord;
        _auditLog = auditLog;
    }

    public async Task<IReadOnlyList<ContractTemplateResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var templates = await _repository.GetAllByLandlordAsync(landlordId, cancellationToken);
        var categories = await _categoryRepository.GetAllByLandlordAsync(landlordId, cancellationToken);
        var categoryNameById = categories.ToDictionary(c => c.Id, c => c.Name);
        return templates.Select(t => ToResponse(t, categoryNameById)).ToList();
    }

    public async Task<ContractTemplateResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var template = await GetOwnedOrThrowAsync(id, cancellationToken);
        var category = await _categoryRepository.GetByIdAsync(template.CategoryId, cancellationToken);
        return ToResponse(template, category?.Name ?? "—");
    }

    public async Task<ContractTemplateResponse> CreateAsync(CreateContractTemplateRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new AppValidationException("El nombre de la plantilla es obligatorio.");
        if (string.IsNullOrWhiteSpace(request.Content))
            throw new AppValidationException("El contenido de la plantilla es obligatorio.");

        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);

        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), request.CategoryId);
        if (category.LandlordId != landlordId)
            throw new ForbiddenException("La categoría no pertenece al negocio autenticado.");

        // La primera plantilla que se crea para una categoría queda activa automáticamente
        // (así siempre hay algo configurado apenas se crea la primera); las siguientes se
        // crean inactivas y el usuario elige explícitamente cuál activar (botón "Activar").
        var existingActive = await _repository.GetActiveByCategoryAsync(landlordId, request.CategoryId, cancellationToken);

        var template = new ContractTemplate
        {
            LandlordId = landlordId,
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Content = request.Content,
            IsActive = existingActive is null,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentLandlord.UserId
        };

        template.Id = await _repository.CreateAsync(template, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Create, "COM_ContractTemplates", template.Id.ToString(), null, template, cancellationToken);
        return ToResponse(template, category.Name);
    }

    public async Task<ContractTemplateResponse> UpdateAsync(int id, UpdateContractTemplateRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new AppValidationException("El nombre de la plantilla es obligatorio.");
        if (string.IsNullOrWhiteSpace(request.Content))
            throw new AppValidationException("El contenido de la plantilla es obligatorio.");

        var template = await GetOwnedOrThrowAsync(id, cancellationToken);

        template.Name = request.Name.Trim();
        template.Content = request.Content;
        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedBy = _currentLandlord.UserId;

        await _repository.UpdateAsync(template, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Update, "COM_ContractTemplates", template.Id.ToString(), null, template, cancellationToken);

        var category = await _categoryRepository.GetByIdAsync(template.CategoryId, cancellationToken);
        return ToResponse(template, category?.Name ?? "—");
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var template = await GetOwnedOrThrowAsync(id, cancellationToken);
        await _repository.DeleteAsync(id, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Delete, "COM_ContractTemplates", template.Id.ToString(), template, null, cancellationToken);
    }

    public async Task<ContractTemplateResponse> ActivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var template = await GetOwnedOrThrowAsync(id, cancellationToken);
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);

        await _repository.DeactivateAllForCategoryAsync(landlordId, template.CategoryId, cancellationToken);

        template.IsActive = true;
        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedBy = _currentLandlord.UserId;
        await _repository.UpdateAsync(template, cancellationToken);
        await _auditLog.LogAsync(AuditActionType.Update, "COM_ContractTemplates", template.Id.ToString(), null, template, cancellationToken);

        var category = await _categoryRepository.GetByIdAsync(template.CategoryId, cancellationToken);
        return ToResponse(template, category?.Name ?? "—");
    }

    private async Task<ContractTemplate> GetOwnedOrThrowAsync(int id, CancellationToken cancellationToken)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);
        var template = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(ContractTemplate), id);

        if (template.LandlordId != landlordId)
            throw new ForbiddenException("La plantilla no pertenece al negocio autenticado.");

        return template;
    }

    private static ContractTemplateResponse ToResponse(ContractTemplate template, IReadOnlyDictionary<int, string> categoryNameById) =>
        ToResponse(template, categoryNameById.GetValueOrDefault(template.CategoryId, "—"));

    private static ContractTemplateResponse ToResponse(ContractTemplate template, string categoryName) => new(
        template.Id, template.CategoryId, categoryName, template.Name, template.Content, template.IsActive, template.CreatedAt);
}
