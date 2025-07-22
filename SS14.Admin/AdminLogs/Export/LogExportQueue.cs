using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace SS14.Admin.AdminLogs.Export;

public sealed class LogExportQueue
{
    private readonly IOptions<ExportConfiguration> _configuration;
    private readonly Channel<ExportProcessItem> _queue;

    private readonly List<Channel<string>> _reportChannels = [];

    public int MaxItemCount => _configuration.Value.ProcessQueueMaxSize;
    public int Count => _queue.Reader.Count;

    public LogExportQueue(IOptions<ExportConfiguration> configuration)
    {
        _configuration = configuration;

        var channelConfiguration = new BoundedChannelOptions(MaxItemCount)
        {
            FullMode = BoundedChannelFullMode.DropWrite
        };

        _queue = Channel.CreateBounded<ExportProcessItem>(channelConfiguration);
    }

    public async Task<bool> TryQueueProcessItem(ExportProcessItem item)
    {
        if (Count == MaxItemCount)
            return false;

        await _queue.Writer.WriteAsync(item);
        return true;
    }

    public async ValueTask<ExportProcessItem> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }

    public ReportChannel CreateReportChannel()
    {
        var channelOptions = new BoundedChannelOptions(1)
        {
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.DropOldest,
        };

        var channel = Channel.CreateBounded<string>(channelOptions);
        _reportChannels.Add(channel);

        return new ReportChannel(
            channel,
            new WeakReference<IList<Channel<string>>>(_reportChannels)
        );
    }

    public async Task ReportFinishedExport(string filename)
    {
        foreach (var channel in _reportChannels)
        {
            await channel.Writer.WriteAsync(filename);
        }
    }

    public sealed record ReportChannel : IDisposable
    {
        private readonly Channel<string> _channel;
        private readonly WeakReference<IList<Channel<string>>> _channels;

        public ReportChannel(Channel<string> channel, WeakReference<IList<Channel<string>>> channels)
        {
            _channel = channel;
            _channels = channels;
        }


        public async ValueTask<string> Listen(CancellationToken ct)
        {
            return await _channel.Reader.ReadAsync(ct);
        }

        public void Dispose()
        {
            _channel.Writer.TryComplete();

            if (!_channels.TryGetTarget(out var channels))
                return;

            channels.Remove(_channel);
        }
    }
}
