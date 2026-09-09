using Alkiman.Domain.Common;

namespace Alkiman.Domain.Entities;

/// <summary>Cita agendada en la barbería, desde el portal o desde el sistema. Tabla: BRB_Appointments.</summary>
public class BarbershopAppointment : IAuditable
{
    public Guid Id { get; set; }
    public Guid LandlordId { get; set; }
    public Guid? StylistId { get; set; }
    public int? ServiceId { get; set; }
    public Guid? PortalLinkId { get; set; }
    public string TrackingToken { get; set; } = default!;
    public string ClientName { get; set; } = default!;
    public string? ClientPhone { get; set; }
    public string? ClientEmail { get; set; }
    public string? Notes { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string Source { get; set; } = "Manual"; // "Manual" | "Portal"
    public string Status { get; set; } = "Scheduled"; // "Scheduled" | "Confirmed" | "InProgress" | "Completed" | "Cancelled"
    public bool IsPaid { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
