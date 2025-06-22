using System.Text.Json;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.Extensions.Options;

namespace SS14.Admin.AdminLogs.Export;

public static class LogExportExtension
{
    public static void MapLogExportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/logs/export/poll",  async (CancellationToken ct, LogExportQueue queue) =>
        {
            var timoutCt = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timoutCt.CancelAfter(TimeSpan.FromSeconds(120));
            using var channel = queue.CreateReportChannel();
            var filename = await channel.Listen(timoutCt.Token);

            return Results.Ok(filename);
        }).RequireAuthorization();

        endpoints.MapGet("/logs/export/list", async (IOptions<ExportConfiguration> config) =>
        {
            var exportPath = Path.Combine(Directory.GetCurrentDirectory(), config.Value.ExportDirectory);
            var files = Directory.EnumerateFiles(exportPath, "*.csv*");

            return Results.Ok(files.Select(Path.GetFileName));

        }).RequireAuthorization();

        endpoints.MapGet("/logs/export/download/{filename}", async (string filename, IOptions<ExportConfiguration> config) =>
        {
            var extension = Path.GetExtension(filename);
            var mimetype = extension switch
            {
                ".gz" => "application/x-gzip",
                ".csv" => "text/csv",
                _ => null
            };

            if (mimetype == null)
                return Results.NotFound();
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), config.Value.ExportDirectory);
            var path = Path.Combine(basePath, filename);
            return !File.Exists(path) ? Results.NotFound() : Results.File(path, contentType: mimetype);
        }).RequireAuthorization();
    }
}
