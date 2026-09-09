namespace Alkiman.Application.Reports;

public interface IReportService
{
    /// <param name="from">Inclusive start date for time-based metrics (income, expenses, rankings). Null means no lower bound.</param>
    /// <param name="to">Inclusive end date. Null means no upper bound.</param>
    Task<ReportSummaryResponse> GetSummaryAsync(DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default);
}
