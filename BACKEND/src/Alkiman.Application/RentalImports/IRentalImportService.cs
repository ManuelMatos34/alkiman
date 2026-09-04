namespace Alkiman.Application.RentalImports;

public interface IRentalImportService
{
    /// <summary>
    /// Importa un lote de alquileres ya existentes (llevados manualmente o en otro sistema).
    /// Cliente, categoría y activo se matchean por identificación/nombre dentro del negocio
    /// actual y, si no existen, se crean automáticamente para permitir una migración rápida.
    /// </summary>
    Task<ImportRentalsResponse> ImportAsync(ImportRentalsRequest request, CancellationToken cancellationToken = default);
}
