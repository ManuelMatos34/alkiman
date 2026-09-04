using Alkiman.Domain.Common;

namespace Alkiman.Domain.Entities;

/// <summary>
/// Directorio de lavadores del negocio: a quién se le asigna un vehículo y a
/// nombre de quién queda el historial. Tabla: CWS_Washers.
///
/// Es una entidad DEL MÓDULO, no una identidad del sistema —el mismo lugar que
/// ocupa <see cref="Customer"/> en Alquileres—. Un lavador no necesita cuenta:
/// el personal de un lavadero rota, y obligar a crear un usuario con email y
/// contraseña por cada persona que pasa una franela no tiene sentido.
///
/// Ver <see cref="UserId"/> para el caso en que sí la necesita.
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

    /// <summary>
    /// Vínculo OPCIONAL con una cuenta del sistema (CFG_Users).
    ///
    /// null —lo normal— es un lavador que no entra al software: se le asigna
    /// trabajo desde el tablero y nada más. Con valor, esa persona además inicia
    /// sesión y mueve la cola por su cuenta, que es para lo que existe el rol de
    /// sistema "Lavador".
    ///
    /// La cuenta y el lavador siguen siendo cosas distintas: dar de baja al
    /// usuario no borra el historial del lavador.
    /// </summary>
    public Guid? UserId { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
