namespace Alkiman.Application.Reports;

public interface IReportService
{
    Task<ReportSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default);
}
