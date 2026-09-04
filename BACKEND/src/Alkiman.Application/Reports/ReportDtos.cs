namespace Alkiman.Application.Reports;

// ---- Respuesta pública (lo que consume el frontend) ----

public record ReportSummaryResponse(
    FinancialSummaryDto Financial,
    RentalsSummaryDto Rentals,
    AssetsSummaryDto Assets,
    IReadOnlyList<CategoryRevenueDto> RevenueByCategory,
    IReadOnlyList<MonthlyFinancialDto> RevenueByMonth,
    IReadOnlyList<OverdueRentalDto> OverdueRentals,
    IReadOnlyList<AssetRevenueDto> TopAssets,
    IReadOnlyList<CustomerRevenueDto> TopCustomers
);

public record FinancialSummaryDto(
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal NetProfit,
    decimal AverageRentalPrice,
    decimal TotalContractedValue
);

public record RentalsSummaryDto(int Total, int Active, int Completed, int Overdue);

public record AssetsSummaryDto(
    int Total,
    int Available,
    int Rented,
    int Maintenance,
    decimal UtilizationRatePercent,
    decimal InventoryValue
);

public record CategoryRevenueDto(string CategoryName, decimal Revenue, int RentalsCount);

public record MonthlyFinancialDto(string Month, decimal Income, decimal Expenses);

public record OverdueRentalDto(
    Guid RentalId,
    string AssetName,
    string CustomerName,
    DateTime EndDate,
    int DaysOverdue,
    decimal TotalPrice
);

public record AssetRevenueDto(Guid AssetId, string AssetName, decimal Revenue, int RentalsCount);

public record CustomerRevenueDto(Guid CustomerId, string CustomerName, decimal TotalPaid, int RentalsCount);

// ---- Formas de fila crudas usadas solo por el repositorio (mapeo 1:1 con el SQL) ----

public record FinancialTotalsRaw(decimal TotalIncome, decimal TotalExpenses);

public record RentalsTotalsRaw(int Total, int Active, int Completed, int Overdue, decimal TotalContractedValue);

public record AssetsTotalsRaw(int Total, int Available, int Rented, int Maintenance, decimal InventoryValue);

public record OverdueRentalRaw(
    Guid RentalId,
    string AssetName,
    string CustomerName,
    DateTime EndDate,
    decimal TotalPrice
);
