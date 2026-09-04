namespace Alkiman.Domain.Enums;

/// <summary>Estado transaccional de una renta (columna TRX_Rentals.Status).</summary>
public enum RentalStatus
{
    Active,
    Completed,
    Overdue,
    Cancelled
}
