namespace SS14.Admin.Storage;

public sealed class StorageOptions
{
    public required StorageBackendType Type { get; set; }
    public StorageOptionsLocal? Local { get; set; }
    public StorageOptionsS3? S3 { get; set; }
}

public enum StorageBackendType
{
    Local,
    S3
}

public sealed class StorageOptionsLocal
{
    public required string RootPath { get; init; }
}

public sealed class StorageOptionsS3
{
    public required string BucketName { get; init; }
    public required string AccessKey { get; init; }
    public required string SecretKey { get; init; }

    // S3 configuration goes in the "Config" section under this, which isn't defined in this class.
    public const string LocationS3Config = "Config";
}
