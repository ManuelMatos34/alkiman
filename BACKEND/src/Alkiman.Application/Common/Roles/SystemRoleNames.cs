namespace Alkiman.Application.Common.Roles;

/// <summary>
/// Nombres de los roles que crea el sistema (CFG_Roles.IsSystem = 1). Se comparan
/// con OrdinalIgnoreCase porque el nombre es la única forma de identificarlos: la
/// tabla no tiene una columna de "tipo de rol".
///
/// IsSystem por sí solo NO alcanza para distinguirlos —Administrador y Lavador son
/// los dos IsSystem— y confundirlos fue justamente lo que hizo que los scripts 16 y
/// 17 repartieran permisos de Carwash a roles que no debían tenerlos.
/// </summary>
public static class SystemRoleNames
{
    /// <summary>Dueño del negocio: recibe todos los permisos de los módulos habilitados.</summary>
    public const string Owner = "Administrador";

    /// <summary>Rol operativo de Carwash: ve la cola y avanza vehículos, no administra el catálogo.</summary>
    public const string Washer = "Lavador";
}
