using Alkiman.Domain.Common;

namespace Alkiman.Domain.Entities;

/// <summary>
/// Recordatorio manual (ej: seguimiento a un cliente, activo por vencer) que el
/// negocio quiere gestionar desde la pantalla de Correos. No dispara envíos por
/// sí solo todavía; es un registro de seguimiento. Tabla: COM_Reminders.
/// </summary>
public class Reminder : IAuditable
{
    public Guid Id { get; set; }
    public Guid LandlordId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? RentalId { get; set; }
    public string Title { get; set; } = default!;
    public string? Message { get; set; }
    public DateTime RemindAt { get; set; }
    public string Status { get; set; } = default!; // "Pending" | "Completed" | "Cancelled"

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
