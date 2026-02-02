namespace Inventory.API.Contracts.Reports
{
    public sealed record LowStockReportResponse(
    long Id,
    DateTimeOffset GeneratedAt,
    decimal Threshold,
    string ReportJson
);
}
