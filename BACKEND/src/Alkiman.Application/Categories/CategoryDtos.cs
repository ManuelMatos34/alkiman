namespace Alkiman.Application.Categories;

public record CategoryResponse(int Id, string Name, DateTime CreatedAt);

public record CreateCategoryRequest(string Name);

public record UpdateCategoryRequest(string Name);
