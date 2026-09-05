using Alkiman.Domain.Common;

namespace Alkiman.Domain.Entities;

/// <summary>
/// Un vehículo en la cola de Carwash. <see cref="Source"/> distingue si lo
/// registró el Encargado (Presencial) o el propio cliente (Portal).
/// <see cref="AccessToken"/> resuelve la página pública de estado del turno
/// (mismo patrón que <c>Rental.AccessToken</c>). Tabla: CWS_Tickets.
/// </summary>
public class CarwashTicket : IAuditable
{
    public Guid Id { get; set; }
    public Guid LandlordId { get; set; }
    public Guid CustomerId { get; set; }
    public int ServiceId { get; set; }
    /// <summary>Lavador asignado (<see cref="CarwashWasher"/>), no un usuario del sistema: quien lava no necesita cuenta.</summary>
    public Guid? AssignedToWasherId { get; set; }
    public int QueueNumber { get; set; }
    public string VehiclePlate { get; set; } = default!;
    public string? VehicleBrand { get; set; }
    public string? VehicleModel { get; set; }
    public int? VehicleYear { get; set; }
    public string? VehicleColor { get; set; }
    /// <summary>
    /// Precio del servicio base congelado al dar de alta el ticket. Junto con
    /// <see cref="CarwashTicketExtra.Price"/> forma el total, que no cambia si
    /// después se retoca la lista de precios.
    /// </summary>
    public decimal ServicePrice { get; set; }
    /// <summary>Ver <see cref="CarwashTicketStatus"/> para los valores posibles.</summary>
    public string Status { get; set; } = default!;
    /// <summary>Ver <see cref="CarwashTicketSource"/> para los valores posibles.</summary>
    public string Source { get; set; } = default!;
    public Guid AccessToken { get; set; }
    public DateTime? ArrivalDeadline { get; set; }
    public DateTime? ArrivedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? ReadyAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? Notes { get; set; }

    /// <summary>
    /// Propina que el mostrador confirmó haber recibido al entregar el vehículo.
    /// NULL = no se registró ninguna. Carwash no procesa pagos, así que esto no
    /// cobra nada: deja constancia de plata que cambió de manos en efectivo.
    /// </summary>
    public decimal? TipAmount { get; set; }

    /// <summary>
    /// A quién se le atribuyó la propina, congelado al entregar. Separado de
    /// <see cref="AssignedToWasherId"/> a propósito, por el mismo motivo que
    /// <see cref="ServicePrice"/> no se lee del catálogo de servicios: si el
    /// ranking leyera la columna operativa, reasignar un ticket ya entregado le
    /// movería lo ganado de una persona a otra sin que nadie lo note.
    /// </summary>
    public Guid? TipWasherId { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Estados posibles de <see cref="CarwashTicket.Status"/> (columna
/// NVARCHAR + CHECK constraint en CWS_Tickets, no una tabla de catálogo
/// aparte -- mismo criterio simple que el resto de estados "planos" del
/// proyecto).
/// </summary>
public static class CarwashTicketStatus
{
    public const string Waiting = "Waiting";
    public const string ArrivalPending = "ArrivalPending";
    public const string InProgress = "InProgress";
    public const string Drying = "Drying";
    /// <summary>Encerado: paso OPCIONAL entre <see cref="Drying"/> y <see cref="Ready"/>; solo aplica si el ticket lleva el extra correspondiente.</summary>
    public const string Waxing = "Waxing";
    public const string Ready = "Ready";
    public const string Delivered = "Delivered";
    public const string Cancelled = "Cancelled";
    public const string Expired = "Expired";
}

/// <summary>Origen posible de <see cref="CarwashTicket.Source"/>.</summary>
public static class CarwashTicketSource
{
    public const string Presencial = "Presencial";
    public const string Portal = "Portal";
}
