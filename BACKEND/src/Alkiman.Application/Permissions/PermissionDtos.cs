namespace Alkiman.Application.Permissions;

/// <param name="Module">Etiqueta de agrupación visual para el editor de roles.</param>
/// <param name="ModuleCode">
/// Módulo al que pertenece el permiso, o null si es de plataforma. El editor de
/// roles lo usa para ocultar los permisos de módulos que el negocio no habilitó.
/// </param>
public record PermissionResponse(int Id, string Code, string Module, string Description, string? ModuleCode);
