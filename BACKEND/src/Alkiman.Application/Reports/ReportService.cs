using Alkiman.Application.Common.Interfaces;

namespace Alkiman.Application.Reports;

/// <summary>Arma el tablero de métricas/reportes del negocio autenticado a partir de agregaciones de solo lectura.</summary>
public class ReportService : IReportService
{
    private const int TopRankingSize = 5;
    private const int RevenueByMonthWindowMonths = 6;

    private readonly IReportRepository _repository;
    private readonly ICurrentLandlordService _currentLandlord;

    public ReportService(IReportRepository repository, ICurrentLandlordService currentLandlord)
    {
        _repository = repository;
        _currentLandlord = currentLandlord;
    }

    public async Task<ReportSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var landlordId = await _currentLandlord.GetCurrentLandlordIdAsync(cancellationToken);

        var financial = await _repository.GetFinancialTotalsAsync(landlordId, cancellationToken);
        var rentals = await _repository.GetRentalsTotalsAsync(landlordId, cancellationToken);
        var assets = await _repository.GetAssetsTotalsAsync(landlordId, cancellationToken);
        var revenueByCategory = await _repository.GetRevenueByCategoryAsync(landlordId, cancellationToken);

        var now = DateTime.UtcNow;
        var fromDate = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddMonths(-(RevenueByMonthWindowMonths - 1));
        var revenueByMonthRaw = await _repository.GetRevenueByMonthAsync(landlordId, fromDate, cancellationToken);
        var revenueByMonth = FillMonthGaps(revenueByMonthRaw, fromDate, RevenueByMonthWindowMonths);

        var overdueRaw = await _repository.GetOverdueRentalsAsync(landlordId, cancellationToken);
        var topAssets = await _repository.GetTopAssetsAsync(landlordId, TopRankingSize, cancellationToken);
        var topCustomers = await _repository.GetTopCustomersAsync(landlordId, TopRankingSize, cancellationToken);

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

    /// <summary>Completa con ceros los meses del rango que no tuvieron ningún movimiento, para que el gráfico no tenga huecos.</summary>
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
