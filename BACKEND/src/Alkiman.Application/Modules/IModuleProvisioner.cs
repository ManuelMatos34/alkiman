namespace Alkiman.Application.Modules;

/// <summary>
/// Siembra lo que un negocio necesita para empezar a usar un módulo: hoy, los
/// permisos del módulo en el rol Administrador.
///
/// Es el único lugar que reparte permisos de módulo. Recibe el landlordId como
/// parámetro —en vez de sacarlo del usuario autenticado— porque también corre
/// durante el registro, cuando todavía no hay request con token.
/// </summary>
public interface IModuleProvisioner
{
    /// <summary>
    /// Provisión completa de un módulo recién habilitado. Idempotente: se puede
    /// repetir sin duplicar permisos.
    /// </summary>
    Task ProvisionAsync(Guid landlordId, string moduleCode, CancellationToken cancellationToken = default);
}
