namespace Alkiman.Application.Common.Roles;

/// <summary>
/// Nombres de los roles que crea el sistema (CFG_Roles.IsSystem = 1). Se comparan
/// con OrdinalIgnoreCase porque el nombre es la única forma de identificarlos: la
/// tabla no tiene una columna de "tipo de rol".
///
/// Hoy queda uno solo. El rol "Lavador" que sembraba Carwash se quitó (ver el
/// script 25): sembrar roles automáticamente le llenaba el mantenimiento de roles
/// al negocio con uno que no había pedido y que además no podía borrar por ser
/// IsSystem. Quien necesite un usuario que trabaje la cola se arma el rol a mano
/// con los permisos carwash.view y carwash.work.
/// </summary>
public static class SystemRoleNames
{
    /// <summary>Dueño del negocio: recibe todos los permisos de los módulos habilitados.</summary>
    public const string Owner = "Administrador";
}
