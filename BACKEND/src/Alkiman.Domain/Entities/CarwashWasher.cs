using Alkiman.Domain.Common;

namespace Alkiman.Domain.Entities;

/// <summary>
/// Directorio de lavadores del negocio: a quién se le asigna un vehículo y a
/// nombre de quién queda el historial. Tabla: CWS_Washers.
///
/// Es una entidad DEL MÓDULO, no una identidad del sistema —el mismo lugar que
/// ocupa <see cref="Customer"/> en Alquileres—. Un lavador no tiene cuenta:
/// el personal de un lavadero rota, y obligar a crear un usuario con email y
/// contraseña por cada persona que pasa una franela no tiene sentido.
///
/// La separación es total y sin excepciones: no hay forma de vincular esta ficha
/// con una fila de CFG_Users. Existió una (CWS_Washers.UserId) y se eliminó en el
/// script 21 porque preguntarle al usuario por una "cuenta" al dar de alta a un
/// lavador mezclaba dos conceptos que no tienen por qué mezclarse.
///
/// Quien además tenga que entrar al software es, simplemente, un usuario del
/// sistema con el rol "Lavador" (permiso carwash.work). Son dos altas separadas
/// porque son dos cosas separadas.
/// </summary>
public class CarwashWasher : IAuditable
{
    public Guid Id { get; set; }
    public Guid LandlordId { get; set; }
    public string FullName { get; set; } = default!;
    public string? Phone { get; set; }

    /// <summary>
    /// Baja lógica. Un lavador inactivo no se puede asignar a trabajo nuevo,
    /// pero su nombre tiene que seguir apareciendo en los tickets que ya lavó:
    /// por eso se desactiva en vez de borrarse.
    /// </summary>
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
