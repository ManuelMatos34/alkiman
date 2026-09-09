using Alkiman.Application.Common.Interfaces;
using Alkiman.Application.Reports;
using Dapper;

namespace Alkiman.Infrastructure.Repositories;

/// <summary>Agregaciones de solo lectura (SUM/COUNT/GROUP BY) para el tablero de métricas. No modifica datos.</summary>
public class ReportRepository : IReportRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ReportRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<FinancialTotalsRaw> GetFinancialTotalsAsync(Guid landlordId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT
                ISNULL(SUM(CASE WHEN Type = 'Income'  THEN Amount ELSE 0 END), 0) AS TotalIncome,
                ISNULL(SUM(CASE WHEN Type = 'Expense' THEN Amount ELSE 0 END), 0) AS TotalExpenses
            FROM dbo.TRX_Payments
            WHERE LandlordId = @LandlordId
              AND (@From IS NULL OR PaymentDate >= @From)
              AND (@To   IS NULL OR PaymentDate <= @To)
            """;
        return await connection.QuerySingleAsync<FinancialTotalsRaw>(sql,
            new { LandlordId = landlordId, From = from, To = to });
    }

    public async Task<RentalsTotalsRaw> GetRentalsTotalsAsync(Guid landlordId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT
                COUNT(*)                                                                                       AS Total,
                ISNULL(SUM(CASE WHEN r.Status = 'Active' THEN 1 ELSE 0 END), 0)                                 AS Active,
                ISNULL(SUM(CASE WHEN r.Status = 'Completed' THEN 1 ELSE 0 END), 0)                              AS Completed,
                ISNULL(SUM(CASE WHEN r.Status = 'Overdue'
                          OR (r.Status = 'Active' AND r.EndDate < SYSUTCDATETIME()) THEN 1 ELSE 0 END), 0)      AS Overdue,
                ISNULL(SUM(r.TotalPrice), 0)                                                                    AS TotalContractedValue
            FROM dbo.TRX_Rentals r
            INNER JOIN dbo.INV_Assets a ON a.Id = r.AssetId
            WHERE a.LandlordId = @LandlordId
            """;
        return await connection.QuerySingleAsync<RentalsTotalsRaw>(sql, new { LandlordId = landlordId });
    }

    public async Task<AssetsTotalsRaw> GetAssetsTotalsAsync(Guid landlordId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT
                COUNT(*)                                                          AS Total,
                ISNULL(SUM(CASE WHEN Status = 'Available'    THEN 1 ELSE 0 END), 0) AS Available,
                ISNULL(SUM(CASE WHEN Status = 'Rented'       THEN 1 ELSE 0 END), 0) AS Rented,
                ISNULL(SUM(CASE WHEN Status = 'Maintenance'  THEN 1 ELSE 0 END), 0) AS Maintenance,
                ISNULL(SUM(BasePrice * Stock), 0)                                   AS InventoryValue
            FROM dbo.INV_Assets
            WHERE LandlordId = @LandlordId
            """;
        return await connection.QuerySingleAsync<AssetsTotalsRaw>(sql, new { LandlordId = landlordId });
    }

    public async Task<IReadOnlyList<CategoryRevenueDto>> GetRevenueByCategoryAsync(Guid landlordId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT
                c.Name                    AS CategoryName,
                ISNULL(SUM(p.Amount), 0)   AS Revenue,
                COUNT(DISTINCT r.Id)       AS RentalsCount
            FROM dbo.CFG_Categories c
            LEFT JOIN dbo.INV_Assets a   ON a.CategoryId = c.Id
            LEFT JOIN dbo.TRX_Rentals r  ON r.AssetId = a.Id
            LEFT JOIN dbo.TRX_Payments p ON p.RentalId = r.Id AND p.Type = 'Income'
                                         AND (@From IS NULL OR p.PaymentDate >= @From)
                                         AND (@To   IS NULL OR p.PaymentDate <= @To)
            WHERE c.LandlordId = @LandlordId
            GROUP BY c.Name
            ORDER BY Revenue DESC
            """;
        var result = await connection.QueryAsync<CategoryRevenueDto>(sql,
            new { LandlordId = landlordId, From = from, To = to });
        return result.ToList();
    }

    public async Task<IReadOnlyList<MonthlyFinancialDto>> GetRevenueByMonthAsync(Guid landlordId, DateTime fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT
                FORMAT(PaymentDate, 'yyyy-MM')                                      AS Month,
                ISNULL(SUM(CASE WHEN Type = 'Income'  THEN Amount ELSE 0 END), 0)   AS Income,
                ISNULL(SUM(CASE WHEN Type = 'Expense' THEN Amount ELSE 0 END), 0)   AS Expenses
            FROM dbo.TRX_Payments
            WHERE LandlordId = @LandlordId
              AND PaymentDate >= @FromDate
              AND (@ToDate IS NULL OR PaymentDate <= @ToDate)
            GROUP BY FORMAT(PaymentDate, 'yyyy-MM')
            ORDER BY Month
            """;
        var result = await connection.QueryAsync<MonthlyFinancialDto>(sql,
            new { LandlordId = landlordId, FromDate = fromDate, ToDate = toDate });
        return result.ToList();
    }

    public async Task<IReadOnlyList<OverdueRentalRaw>> GetOverdueRentalsAsync(Guid landlordId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string sql = """
            SELECT
                r.Id        AS RentalId,
                a.Name      AS AssetName,
                cu.FullName AS CustomerName,
                r.EndDate,
                r.TotalPrice
            FROM dbo.TRX_Rentals r
            INNER JOIN dbo.INV_Assets a      ON a.Id = r.AssetId
            INNER JOIN dbo.CRM_Customers cu  ON cu.Id = r.CustomerId
            WHERE a.LandlordId = @LandlordId
              AND (r.Status = 'Overdue' OR (r.Status = 'Active' AND r.EndDate < SYSUTCDATETIME()))
            ORDER BY r.EndDate ASC
            """;
        var result = await connection.QueryAsync<OverdueRentalRaw>(sql, new { LandlordId = landlordId });
        return result.ToList();
    }

    public async Task<IReadOnlyList<AssetRevenueDto>> GetTopAssetsAsync(Guid landlordId, int top, DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT TOP (@Top)
                a.Id                     AS AssetId,
                a.Name                   AS AssetName,
                ISNULL(SUM(p.Amount), 0) AS Revenue,
                COUNT(DISTINCT r.Id)     AS RentalsCount
            FROM dbo.INV_Assets a
            LEFT JOIN dbo.TRX_Rentals r  ON r.AssetId = a.Id
            LEFT JOIN dbo.TRX_Payments p ON p.RentalId = r.Id AND p.Type = 'Income'
                                         AND (@From IS NULL OR p.PaymentDate >= @From)
                                         AND (@To   IS NULL OR p.PaymentDate <= @To)
            WHERE a.LandlordId = @LandlordId
            GROUP BY a.Id, a.Name
            ORDER BY Revenue DESC
            """;
        var result = await connection.QueryAsync<AssetRevenueDto>(sql,
            new { LandlordId = landlordId, Top = top, From = from, To = to });
        return result.ToList();
    }

    public async Task<IReadOnlyList<CustomerRevenueDto>> GetTopCustomersAsync(Guid landlordId, int top, DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var sql = $"""
            SELECT TOP (@Top)
                cu.Id                    AS CustomerId,
                cu.FullName              AS CustomerName,
                ISNULL(SUM(p.Amount), 0) AS TotalPaid,
                COUNT(DISTINCT r.Id)     AS RentalsCount
            FROM dbo.CRM_Customers cu
            LEFT JOIN dbo.TRX_Rentals r  ON r.CustomerId = cu.Id
            LEFT JOIN dbo.TRX_Payments p ON p.RentalId = r.Id AND p.Type = 'Income'
                                         AND (@From IS NULL OR p.PaymentDate >= @From)
                                         AND (@To   IS NULL OR p.PaymentDate <= @To)
            WHERE cu.LandlordId = @LandlordId
            GROUP BY cu.Id, cu.FullName
            ORDER BY TotalPaid DESC
            """;
        var result = await connection.QueryAsync<CustomerRevenueDto>(sql,
            new { LandlordId = landlordId, Top = top, From = from, To = to });
        return result.ToList();
    }
}
