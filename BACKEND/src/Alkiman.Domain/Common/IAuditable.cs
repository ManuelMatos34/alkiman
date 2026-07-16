namespace Alkiman.Domain.Common;

/// <summary>
/// Contrato para las entidades que siguen la convención de auditoría
/// estándar del proyecto (CreatedAt/By, UpdatedAt/By).
/// </summary>
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    string CreatedBy { get; set; }
    DateTime? UpdatedAt { get; set; }
    string? UpdatedBy { get; set; }
}
