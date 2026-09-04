namespace Alkiman.Domain.Entities;

/// <summary>
/// Un extra elegido en un ticket concreto. <see cref="Name"/> y
/// <see cref="Price"/> son un SNAPSHOT tomado de
/// <see cref="CarwashServiceExtra"/> al dar de alta el ticket: no se vuelven a
/// leer del catálogo, así cambiar la lista de precios no reescribe lo que ya
/// se cobró. No implementa IAuditable: es una fila de detalle inmutable que
/// vive y muere con su ticket. Tabla: CWS_TicketExtras.
/// </summary>
public class CarwashTicketExtra
{
    public Guid TicketId { get; set; }
    public int ExtraId { get; set; }
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }
}
