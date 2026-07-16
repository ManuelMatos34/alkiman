using Alkiman.Domain.Common;
using Alkiman.Domain.Enums;

namespace Alkiman.Domain.Entities;

/// <summary>Libro diario de ingresos (rentas) y egresos (gastos). Tabla: TRX_Payments.</summary>
public class Payment : IAuditable
{
    public Guid Id { get; set; }
    public Guid? RentalId { get; set; }
    public Guid LandlordId { get; set; }
    public decimal Amount { get; set; }
    public PaymentType Type { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? StripeTransactionId { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
