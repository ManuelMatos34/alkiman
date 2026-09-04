namespace Alkiman.Application.Reports;

/// <summary>Acceso a datos de solo lectura para el módulo de métricas/reportes. Toda consulta se filtra por LandlordId.</summary>
public interface IReportRepository
{
    Task<FinancialTotalsRaw> GetFinancialTotalsAsync(Guid landlordId, CancellationToken cancellationToken = default);

    Task<RentalsTotalsRaw> GetRentalsTotalsAsync(Guid landlordId, CancellationToken cancellationToken = default);

    Task<AssetsTotalsRaw> GetAssetsTotalsAsync(Guid landlordId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryRevenueDto>> GetRevenueByCategoryAsync(Guid landlordId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MonthlyFinancialDto>> GetRevenueByMonthAsync(Guid landlordId, DateTime fromDate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OverdueRentalRaw>> GetOverdueRentalsAsync(Guid landlordId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AssetRevenueDto>> GetTopAssetsAsync(Guid landlordId, int top, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerRevenueDto>> GetTopCustomersAsync(Guid landlordId, int top, CancellationToken cancellationToken = default);
}
