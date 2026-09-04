using Alkiman.Domain.Common;
using Alkiman.Domain.Enums;

namespace Alkiman.Domain.Entities;

/// <summary>Pedido de prórroga o cancelación que un cliente hace desde su link público de renta, pendiente de revisión del negocio. Tabla: TRX_RentalRequests.</summary>
public class RentalRequest : IAuditable
{
    public Guid Id { get; set; }
    public Guid LandlordId { get; set; }
    public Guid RentalId { get; set; }
    public RentalRequestType Type { get; set; }
    public RentalRequestStatus Status { get; set; }
    /// <summary>Cantidad de períodos pedidos; solo Extension.</summary>
    public int? RequestedPeriods { get; set; }
    /// <summary>Vista previa calculada al momento del pedido, solo Extension.</summary>
    public DateTime? ProposedEndDate { get; set; }
    public string? Reason { get; set; }
    public string? StaffNote { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
