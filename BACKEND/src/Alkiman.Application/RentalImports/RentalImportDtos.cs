namespace Alkiman.Application.RentalImports;

/// <summary>
/// Fila cruda de un CSV/Excel exportado de otro sistema o de una planilla manual. Todos los
/// campos numéricos/fecha viajan como texto: la validación y el parseo real ocurren en el
/// servicio, fila por fila, igual que en el resto de los formularios de la aplicación.
/// </summary>
public record RentalImportRow(
    string CustomerFullName,
    string CustomerIdentityNumber,
    string? CustomerPhone,
    string? CustomerEmail,
    string AssetName,
    string? AssetCategoryName,
    string? AssetBasePrice,
    string? AssetStock,
    string? AssetRentalType,
    string StartDate,
    string EndDate,
    string TotalPrice,
    string? Status
);

public record ImportRentalsRequest(IReadOnlyList<RentalImportRow> Rows);

/// <summary>Resultado de procesar una fila individual. Una fila fallida nunca aborta el resto del lote.</summary>
public record RentalImportRowResult(
    int RowNumber,
    bool Success,
    string? ErrorMessage,
    Guid? RentalId,
    bool CustomerCreated,
    bool AssetCreated,
    bool CategoryCreated
);

public record ImportRentalsResponse(
    int Total,
    int Succeeded,
    int Failed,
    IReadOnlyList<RentalImportRowResult> Results
);
