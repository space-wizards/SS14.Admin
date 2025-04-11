namespace SS14.Admin.Storage;

public abstract class StorageType;

public sealed class StorageTemp : StorageType
{
    public const string Position = "TempStorage";
}
