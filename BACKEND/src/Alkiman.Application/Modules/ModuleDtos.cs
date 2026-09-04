namespace Alkiman.Application.Modules;

public record ModuleResponse(
    string Code,
    string Name,
    string? Description,
    string IconName,
    bool IsAvailable,
    bool IsEnabled);
