namespace Alkiman.Application.Modules;

/// <summary>
/// Siembra lo que un negocio necesita para empezar a usar un módulo: los permisos
/// del módulo en el rol Administrador y los roles de sistema que el módulo declara
/// en <c>ModuleProvisioningCatalog</c>.
///
/// Es el único lugar que reparte permisos de módulo. Recibe landlordId y createdBy
/// como parámetros —en vez de sacarlos del usuario autenticado— porque también corre
/// durante el registro, cuando todavía no hay request con token.
/// </summary>
public interface IModuleProvisioner
{
    /// <summary>
    /// Provisión completa de un módulo recién habilitado. Idempotente: se puede
    /// repetir sin duplicar roles ni permisos.
    /// </summary>
    Task ProvisionAsync(Guid landlordId, string moduleCode, string createdBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Crea un rol de sistema declarado por el módulo, si el negocio todavía no lo tiene.
    /// Lo usan los roles con <c>ProvisionOnEnable = false</c>, que dependen de cómo el
    /// negocio configure el módulo (ej.: "Lavador" solo en modo Empresa).
    /// </summary>
    Task EnsureSystemRoleAsync(Guid landlordId, string moduleCode, string roleName, string createdBy, CancellationToken cancellationToken = default);
}
