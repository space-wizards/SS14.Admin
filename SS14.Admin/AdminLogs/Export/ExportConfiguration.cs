namespace SS14.Admin.AdminLogs.Export;

public class ExportConfiguration
{
    public const string Name = "Export";

    /// <summary>
    /// The maximum amount of export processes that can be queued up before new export requests will be rejected
    /// </summary>
    public int ProcessQueueMaxSize { get; set; } = 6;

    /// <summary>
    /// This is the directory for containing generated exports
    /// </summary>
    public string ExportDirectory { get; set; } = "export";
}
