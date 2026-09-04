namespace Alkiman.Domain.Enums;

/// <summary>Estado de revisión de un pedido de prórroga/cancelación (columna TRX_RentalRequests.Status).</summary>
public enum RentalRequestStatus
{
    Pending,
    Approved,
    Rejected
}
