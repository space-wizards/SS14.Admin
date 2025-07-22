using System.IO.Compression;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace SS14.Admin.AdminLogs.Export;

public sealed class LogExporter
{
    private const char ColumnSeparator = ',';
    private const char Quote = '"';
    private const string EscapedQuote = "\"\"";

    private readonly PostgresServerDbContext _context;
    private readonly IOptions<ExportConfiguration> _configuration;

    public LogExporter(PostgresServerDbContext context, IOptions<ExportConfiguration> configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<string> Export(ExportProcessItem item, CancellationToken ct)
    {
        // Prevent accidentally exporting all logs
        if ((!item.FromDate.HasValue || !item.ToDate.HasValue) && !item.RoundId.HasValue)
            throw new Exception("Neither date or round id filter is set correctly for log export.");

        var filename = $"{DateTime.UtcNow.ToShortDateString()}-{Guid.NewGuid()}_log_export.csv{(item.UseCompression ? ".gz" : "")}";
        var path = Path.Combine(_configuration.Value.ExportDirectory, filename);
        await using var fileStream =  new FileStream(path, FileMode.Create, FileAccess.Write);

        if (item.UseCompression)
        {
            await using var compressionStream = new GZipStream(fileStream, CompressionMode.Compress, leaveOpen: true);
            await using var writer = new StreamWriter(compressionStream);
            await WriteCsv(writer, item, ct);
        }
        else
        {
            await using var writer = new StreamWriter(fileStream);
            await WriteCsv(writer, item, ct);
        }

        return filename;
    }

    private async Task WriteCsv(StreamWriter writer, ExportProcessItem item, CancellationToken ct)
    {
        var query = _context.AdminLog
            .AsNoTracking()
            .AsQueryable();

        if (item.RoundId.HasValue)
            query = query.Where(e => e.RoundId == item.RoundId);

        if (item is { FromDate: not null, ToDate: not null })
            query = query.Where(e => item.FromDate.Value.Date.ToUniversalTime() <= e.Date
                                     && e.Date <= item.ToDate.Value.Date.ToUniversalTime());

        if (item.Search != null)
            query = query.Where(e => EF.Functions.ToTsVector("english", e.Message).Matches(item.Search));

        await WriteCsvHeader(writer);

        await foreach (var log in query.AsAsyncEnumerable().WithCancellation(ct))
        {
            await writer.WriteAsync(log.Date.ToString("O"));
            await writer.WriteAsync(ColumnSeparator);
            await writer.WriteAsync(log.Id.ToString());
            await writer.WriteAsync(ColumnSeparator);
            await writer.WriteAsync(log.RoundId.ToString());
            await writer.WriteAsync(ColumnSeparator);
            await writer.WriteAsync(log.Impact.ToString());
            await writer.WriteAsync(ColumnSeparator);
            await writer.WriteAsync(log.Type.ToString());
            await writer.WriteAsync(ColumnSeparator);
            await writer.WriteAsync(Quote);
            await writer.WriteAsync(log.Message.Replace(Quote.ToString(), EscapedQuote));
            await writer.WriteAsync(Quote);
            await writer.WriteAsync(ColumnSeparator);
            await writer.WriteAsync(Quote);
            await writer.WriteAsync(log.Json.RootElement.GetRawText().Replace(Quote.ToString(), EscapedQuote));
            await writer.WriteAsync(Quote);
        }
    }

    private async Task WriteCsvHeader(StreamWriter writer)
    {
        await writer.WriteAsync("timestamp");
        await writer.WriteAsync(ColumnSeparator);
        await writer.WriteAsync("round_id");
        await writer.WriteAsync(ColumnSeparator);
        await writer.WriteAsync("id");
        await writer.WriteAsync(ColumnSeparator);
        await writer.WriteAsync("impact");
        await writer.WriteAsync(ColumnSeparator);
        await writer.WriteAsync("type");
        await writer.WriteAsync(ColumnSeparator);
        await writer.WriteAsync("message");
        await writer.WriteAsync(ColumnSeparator);
        await writer.WriteAsync("json");
    }
}
