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
    /// <summary>
    /// Nombre del cliente congelado al dar de alta el ticket, igual que
    /// <see cref="ServicePrice"/> snapshottea el precio. Así el nombre
    /// que aparece en el tablero no cambia si el registro CRM_Customers
    /// se edita después.
    /// </summary>
    public string? CustomerName { get; set; }
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
    /// Propina del turno. NULL = ninguna.
    ///
    /// Tiene dos orígenes posibles y <see cref="TipPrepaid"/> los distingue: o la
    /// confirmó el mostrador en efectivo al entregar (turnos presenciales), o el
    /// cliente la eligió en el portal y ya se cobró con el servicio (turnos de
    /// portal). El monto vive en la misma columna porque para el ranking de
    /// lavadores es la misma plata; lo que cambia es quién la recibió y cuándo.
    /// </summary>
    public decimal? TipAmount { get; set; }

    /// <summary>
    /// true si <see cref="TipAmount"/> ya entró por el gateway al reservar.
    ///
    /// Existe para que el tablero NO vuelva a pedir la propina al entregar el
    /// vehículo: si lo hiciera, la misma propina quedaría contada dos veces, una
    /// cobrada y otra "recibida en mano" que nunca ocurrió.
    /// </summary>
    public bool TipPrepaid { get; set; }

    /// <summary>Proveedor del cobro online ("Stripe"). NULL en los turnos presenciales, que se pagan en efectivo.</summary>
    public string? PaymentProvider { get; set; }

    /// <summary>Id del PaymentIntent, para conciliar o reembolsar contra el proveedor.</summary>
    public string? PaymentReference { get; set; }

    /// <summary>
    /// Lo que realmente se le cobró a la tarjeta: servicio + extras + propina.
    /// Aparte del total del ticket, que NO incluye propina — mezclarlos rompería
    /// la facturación y el ranking a la vez.
    /// </summary>
    public decimal? PaidAmount { get; set; }

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
