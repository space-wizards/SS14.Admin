namespace SS14.Admin.AdminLogs.Export;

public sealed record ExportProcessItem(
    string? Search = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int? RoundId = null,
    bool UseCompression = false
);
