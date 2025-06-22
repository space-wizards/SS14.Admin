namespace SS14.Admin.AdminLogs.Export;

public sealed class ExportProcessItem
{
    public DateTime? FromDate { get; }
    public DateTime? ToDate { get; }
    public int? RoundId { get; }

    public string? Search { get; }

    public bool UseCompression { get; }

    public ExportProcessItem(string? search = null, DateTime? fromDate = null, DateTime? toDate = null, int? roundId = null, bool useCompression = false)
    {
        Search = search;
        FromDate = fromDate;
        ToDate = toDate;
        UseCompression = useCompression;
        RoundId = roundId;
    }
}
