namespace Alkiman.Domain.Entities;

/// <summary>
/// Comprobante de un checkout completado desde el portal público: qué link se
/// usó, qué renta/cliente generó y el proveedor/referencia de pago verificados
/// server-side antes de crear la renta (Stripe PaymentIntentId).
/// Tabla: PRT_PortalRentals.
/// </summary>
public class PortalRental
{
    public Guid Id { get; set; }
    public Guid PortalLinkId { get; set; }
    public Guid RentalId { get; set; }
    public Guid CustomerId { get; set; }
    public int Quantity { get; set; }

    /// <summary>Cantidad de períodos (según el RentalType del activo al momento de la renta) que el cliente contrató. Ver <see cref="Alkiman.Domain.Common.RentalPeriodCalculator"/>.</summary>
    public int Periods { get; set; }

    /// <summary>Proveedor de pago usado en el checkout: "Stripe".</summary>
    public string PaymentProvider { get; set; } = default!;

    /// <summary>Referencia del pago verificado en el proveedor: PaymentIntentId (Stripe).</summary>
    public string PaymentReference { get; set; } = default!;

    public DateTime CreatedAt { get; set; }
}
