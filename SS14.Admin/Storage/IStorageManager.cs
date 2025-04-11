using System.Buffers;
using Microsoft.AspNetCore.Mvc;

namespace SS14.Admin.Storage;

/// <summary>
/// Abstraction for storing and retrieving files that can either go to disk or some sort of cloud service.
/// </summary>
public interface IStorageManager
{
    /// <summary>
    /// If true, <see cref="MakePublicUrlAsync"/> can be used.
    /// </summary>
    bool CanMakePublicUrls { get; }

    Task CreateFileAsync(string fileName, Stream inputStream, CancellationToken cancel = default);
    Task DeleteFileAsync(string fileName, CancellationToken cancel = default);

    /// <summary>
    /// Make a URL that can be used by anybody to download a given file.
    /// Only available if <see cref="CanMakePublicUrls"/> is true.
    /// </summary>
    /// <param name="fileName">The file to get a URL for.</param>
    /// <param name="expireTime">The time the public URL will expire.</param>
    /// <param name="cancel">Cancellation token.</param>
    Task<string> MakePublicUrlAsync(string fileName, DateTime expireTime, CancellationToken cancel = default);

    /// <summary>
    /// Make an <see cref="IActionResult"/> that will result in a file download response.
    /// </summary>
    /// <param name="fileName">The file to download.</param>
    /// <param name="contentType">HTTP content type for this file download.</param>
    /// <param name="cancel">Cancellation token.</param>
    Task<IActionResult> MakeDownloadResult(string fileName, string contentType, CancellationToken cancel = default);
}

/// <summary>
/// Subtype of the <see cref="IStorageManager"/> interface that adds a type parameter specifying storage category,
/// so we can have multiple of these as services.
/// </summary>
/// <typeparam name="TCategoryName">Type used to represent the category of file storage.</typeparam>
public interface IStorageManager<out TCategoryName> : IStorageManager where TCategoryName : StorageType;

public abstract class BaseStorageManager : IStorageManager
{
    // Bullshit avoidance here.
    private static readonly SearchValues<char> InvalidFilenameChars = SearchValues.Create("\\[]:(){}?\'\"#!@");

    protected static void ValidateFilename(ReadOnlySpan<char> fileName)
    {
        if (fileName.ContainsAny(InvalidFilenameChars))
            throw new ArgumentException("Filename contains invalid characters!");

        if (fileName.IndexOf("..") >= 0)
            throw new ArgumentException("Filename contains invalid characters!");
    }

    public abstract bool CanMakePublicUrls { get; }

    public abstract Task CreateFileAsync(string fileName, Stream inputStream, CancellationToken cancel = default);
    public abstract Task DeleteFileAsync(string fileName, CancellationToken cancel = default);
    public abstract Task<string> MakePublicUrlAsync(string fileName, DateTime expireTime, CancellationToken cancel = default);
    public abstract Task<IActionResult> MakeDownloadResult(string fileName, string contentType, CancellationToken cancel = default);
}
