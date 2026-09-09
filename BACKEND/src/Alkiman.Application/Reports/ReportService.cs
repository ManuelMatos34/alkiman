using Alkiman.Application.Common.Interfaces;

namespace Alkiman.Application.Reports;

/// <summary>Arma el tablero de métricas/reportes del negocio autenticado a partir de agregaciones de solo lectura.</summary>
public class ReportService : IReportService
{
    private const int TopRankingSize = 5;
    private const int DefaultRevenueByMonthWindowMonths = 6;

    private readonly IReportRepository _repository;
    private readonly ICurrentLandlordService _currentLandlord;

    public ReportService(IReportRepository repository, ICurrentLandlordService currentLandlord)
    {
        _repository = repository;
        _currentLandlord = currentLandlord;
    }

    public async Task<ReportSummaryResponse> GetSummaryAsync(DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);

        // Convert DateOnly to DateTime bounds for PaymentDate filtering.
        DateTime? fromUtc = from.HasValue ? from.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) : null;
        DateTime? toUtc   = to.HasValue   ? to.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc)   : null;

        // Snapshot data (current state — not date-filtered).
        var rentals = await _repository.GetRentalsTotalsAsync(landlordId, cancellationToken);
        var assets  = await _repository.GetAssetsTotalsAsync(landlordId, cancellationToken);
        var overdueRaw = await _repository.GetOverdueRentalsAsync(landlordId, cancellationToken);

        // Time-based data (filtered by payment date range when provided).
        var financial         = await _repository.GetFinancialTotalsAsync(landlordId, fromUtc, toUtc, cancellationToken);
        var revenueByCategory = await _repository.GetRevenueByCategoryAsync(landlordId, fromUtc, toUtc, cancellationToken);
        var topAssets         = await _repository.GetTopAssetsAsync(landlordId, TopRankingSize, fromUtc, toUtc, cancellationToken);
        var topCustomers      = await _repository.GetTopCustomersAsync(landlordId, TopRankingSize, fromUtc, toUtc, cancellationToken);

        // Monthly chart: use from/to when provided, otherwise fall back to the last N months.
        var now = DateTime.UtcNow;
        var monthFrom = fromUtc ?? new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddMonths(-(DefaultRevenueByMonthWindowMonths - 1));
        var revenueByMonthRaw = await _repository.GetRevenueByMonthAsync(landlordId, monthFrom, toUtc, cancellationToken);

        int windowMonths = from.HasValue && to.HasValue
            ? Math.Max(1, (to.Value.Year - from.Value.Year) * 12 + to.Value.Month - from.Value.Month + 1)
            : DefaultRevenueByMonthWindowMonths;

        var revenueByMonth = FillMonthGaps(revenueByMonthRaw, monthFrom, windowMonths);

        var financialDto = new FinancialSummaryDto(
            financial.TotalIncome,
            financial.TotalExpenses,
            financial.TotalIncome - financial.TotalExpenses,
            rentals.Total > 0 ? Math.Round(rentals.TotalContractedValue / rentals.Total, 2) : 0m,
            rentals.TotalContractedValue
        );

        var rentalsDto = new RentalsSummaryDto(rentals.Total, rentals.Active, rentals.Completed, rentals.Overdue);

        var assetsDto = new AssetsSummaryDto(
            assets.Total,
            assets.Available,
            assets.Rented,
            assets.Maintenance,
            assets.Total > 0 ? Math.Round((decimal)assets.Rented / assets.Total * 100m, 1) : 0m,
            assets.InventoryValue
        );

        var today = now.Date;
        var overdueRentals = overdueRaw
            .Select(o => new OverdueRentalDto(
                o.RentalId,
                o.AssetName,
                o.CustomerName,
                o.EndDate,
                Math.Max(0, (today - o.EndDate.Date).Days),
                o.TotalPrice))
            .OrderByDescending(o => o.DaysOverdue)
            .ToList();

        return new ReportSummaryResponse(
            financialDto,
            rentalsDto,
            assetsDto,
            revenueByCategory,
            revenueByMonth,
            overdueRentals,
            topAssets,
            topCustomers
        );
    }

    /// <summary>Completa con ceros los meses del rango que no tuvieron movimiento, para que el gráfico no tenga huecos.</summary>
    private static List<MonthlyFinancialDto> FillMonthGaps(
        IReadOnlyList<MonthlyFinancialDto> existing, DateTime fromDate, int windowMonths)
    {
        var byMonth = existing.ToDictionary(m => m.Month);
        var result = new List<MonthlyFinancialDto>(windowMonths);

        for (var i = 0; i < windowMonths; i++)
        {
            var monthKey = fromDate.AddMonths(i).ToString("yyyy-MM");
            result.Add(byMonth.TryGetValue(monthKey, out var found) ? found : new MonthlyFinancialDto(monthKey, 0m, 0m));
        }

        return result;
    }
}
