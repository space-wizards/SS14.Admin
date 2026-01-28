using Serilog;
using ILogger = Serilog.ILogger;

namespace SS14.Admin.AdminLogs.Export;

public sealed class LogExportBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly LogExportQueue _queue;
    private readonly ILogger _log;

    public LogExportBackgroundService(IServiceProvider provider, LogExportQueue queue, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _queue = queue;

        _log = Log.ForContext<LogExportBackgroundService>();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.Information("{ServiceName} started", nameof(LogExportBackgroundService));
        return ProcessQueueAsync(stoppingToken);
    }

    private async Task ProcessQueueAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var item = await _queue.DequeueAsync(ct);
            await ProcessTask(item, ct);
        }
    }

    private async Task ProcessTask(ExportProcessItem item, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var exporter =  scope.ServiceProvider.GetRequiredService<LogExporter>();
        var filename = await exporter.Export(item, ct);
        await _queue.ReportFinishedExport(filename);
    }
}
