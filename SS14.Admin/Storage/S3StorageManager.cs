using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;

namespace SS14.Admin.Storage;

/// <summary>
/// Implementation of <see cref="IStorageManager"/> that stores files on AWS S3-compatible services.
/// </summary>
public class S3StorageManager : BaseStorageManager
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly ILogger _logger;

    public S3StorageManager(IAmazonS3 s3Client, string bucketName, ILogger logger)
    {
        _s3Client = s3Client;
        _bucketName = bucketName;
        _logger = logger;
    }

    public override bool CanMakePublicUrls => true;

    public override async Task CreateFileAsync(string fileName, Stream inputStream, CancellationToken cancel = default)
    {
        await _s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = fileName,
            InputStream = inputStream,
            AutoCloseStream = false,
        }, cancel);
    }

    public override async Task DeleteFileAsync(string fileName, CancellationToken cancel = default)
    {
        await _s3Client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = fileName,
        }, cancel);
    }

    public override Task<string> MakePublicUrlAsync(string fileName, DateTime expireTime, CancellationToken cancel = default)
    {
        var url = _s3Client.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = fileName,
            Verb = HttpVerb.GET,
            Expires = expireTime
        });

        return url;
    }

    public override async Task<IActionResult> MakeDownloadResult(string fileName, string contentType,
        CancellationToken cancel = default)
    {
        var url = await _s3Client.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = fileName,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(1),
        });

        return new RedirectResult(url);
    }
}

public sealed class S3StorageManager<TCategoryName>(
    IAmazonS3 s3Client,
    string bucketName,
    ILogger<S3StorageManager<TCategoryName>> logger)
    : S3StorageManager(s3Client, bucketName, logger), IStorageManager<TCategoryName>
    where TCategoryName : StorageType;
