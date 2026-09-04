using Alkiman.Domain.Common;
using Alkiman.Domain.Enums;

namespace Alkiman.Domain.Entities;

/// <summary>Une a un cliente con un activo durante un lapso pactado. Tabla: TRX_Rentals.</summary>
public class Rental : IAuditable
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public Guid CustomerId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? ContractPdfUrl { get; set; }
    public decimal TotalPrice { get; set; }
    public RentalStatus Status { get; set; }
    /// <summary>Token opaco del link público "mi-renta" (/mi-renta/{AccessToken}) por el que el cliente accede sin loguearse.</summary>
    public Guid AccessToken { get; set; }
    /// <summary>Intentos fallidos consecutivos de verificación de identidad en el link público; junto con <see cref="AccessLockedUntil"/> implementan un lockout simple contra adivinar la identidad del cliente.</summary>
    public int AccessFailedAttempts { get; set; }
    public DateTime? AccessLockedUntil { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
