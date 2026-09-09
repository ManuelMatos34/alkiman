namespace Alkiman.Application.Reports;

/// <summary>Acceso a datos de solo lectura para el módulo de métricas/reportes. Toda consulta se filtra por LandlordId.</summary>
public interface IReportRepository
{
    /// <param name="from">Lower bound for PaymentDate (inclusive). Null = no lower bound.</param>
    /// <param name="to">Upper bound for PaymentDate (inclusive). Null = no upper bound.</param>
    Task<FinancialTotalsRaw> GetFinancialTotalsAsync(Guid landlordId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default);

    Task<RentalsTotalsRaw> GetRentalsTotalsAsync(Guid landlordId, CancellationToken cancellationToken = default);

    Task<AssetsTotalsRaw> GetAssetsTotalsAsync(Guid landlordId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryRevenueDto>> GetRevenueByCategoryAsync(Guid landlordId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MonthlyFinancialDto>> GetRevenueByMonthAsync(Guid landlordId, DateTime fromDate, DateTime? toDate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OverdueRentalRaw>> GetOverdueRentalsAsync(Guid landlordId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AssetRevenueDto>> GetTopAssetsAsync(Guid landlordId, int top, DateTime? from, DateTime? to, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerRevenueDto>> GetTopCustomersAsync(Guid landlordId, int top, DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
}
