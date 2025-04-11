using Microsoft.AspNetCore.Mvc;

namespace SS14.Admin.Storage;

/// <summary>
/// Implementation of <see cref="IStorageManager"/> that stores files on local disk.
/// </summary>
public class LocalStorageManager : BaseStorageManager
{
    private readonly ILogger _logger;

    public string RootPath { get; }

    public LocalStorageManager(StorageOptionsLocal options, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(options.RootPath))
            throw new InvalidOperationException("TempStorage RootPath is empty!");

        _logger = logger;

        RootPath = Path.Combine(Environment.CurrentDirectory, options.RootPath);
        logger.LogDebug("Creating directory {RootPath} if it doesn't already exist", RootPath);
        Directory.CreateDirectory(RootPath);
    }

    public override bool CanMakePublicUrls => false;

    public override async Task CreateFileAsync(string fileName, Stream inputStream, CancellationToken cancel = default)
    {
        var path = Path.Combine(RootPath, fileName);

        await using var finalFile = File.Create(path, 4096, FileOptions.Asynchronous);
        inputStream.Position = 0;
        await inputStream.CopyToAsync(finalFile, cancel);
    }

    public override Task DeleteFileAsync(string fileName, CancellationToken cancel = default)
    {
        var path = Path.Combine(RootPath, fileName);

        File.Delete(path);

        return Task.CompletedTask;
    }

    public override Task<string> MakePublicUrlAsync(string fileName, DateTime expireTime, CancellationToken cancel = default)
    {
        throw new NotSupportedException("Making public URLs is not supported by local storage.");
    }

    public override Task<IActionResult> MakeDownloadResult(string fileName, string contentType,
        CancellationToken cancel = default)
    {
        ValidateFilename(fileName);

        return Task.FromResult<IActionResult>(new PhysicalFileResult(Path.Combine(RootPath, fileName), contentType));
    }
}

public sealed class LocalStorageManager<TCategoryName>(
    StorageOptionsLocal options,
    ILogger<LocalStorageManager<TCategoryName>> logger)
    : LocalStorageManager(options, logger), IStorageManager<TCategoryName>
    where TCategoryName : StorageType;
