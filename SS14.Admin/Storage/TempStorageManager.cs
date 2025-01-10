using System.Buffers;
using Microsoft.Extensions.Options;

namespace SS14.Admin.Storage;

public interface ITempStorageManager
{
    void Initialize();

    string GetFilePath(string fileName);
}

public sealed class TempStorageManager(IOptions<TempStorageOptions> options, ILogger<TempStorageManager> logger)
    : ITempStorageManager
{
    // Bullshit avoidance here.
    private static readonly SearchValues<char> InvalidFilenameChars = SearchValues.Create("\\/[]:(){}?\'\"#!@");

    private readonly TempStorageOptions _options = options.Value;

    public void Initialize()
    {
        if (string.IsNullOrWhiteSpace(_options.RootPath))
            throw new InvalidOperationException("TempStorage RootPath is empty!");

        logger.LogDebug(
            "Creating directory {TempPath} if it doesn't already exist",
            Path.Combine(Environment.CurrentDirectory, _options.RootPath));
        Directory.CreateDirectory(_options.RootPath);
    }

    public string GetFilePath(string fileName)
    {
        if (fileName.AsSpan().ContainsAny(InvalidFilenameChars))
            throw new ArgumentException("Filename contains invalid characters!");

        return Path.Combine(Environment.CurrentDirectory, Path.Combine(_options.RootPath, fileName));
    }
}
