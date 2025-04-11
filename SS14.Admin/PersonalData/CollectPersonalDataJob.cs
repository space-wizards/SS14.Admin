using System.Security.Cryptography;
using SS14.Admin.Data;
using SS14.Admin.JobSystem;
using SS14.Admin.Storage;

namespace SS14.Admin.PersonalData;

[Job("PersonalData.Collect")]
public sealed class CollectPersonalDataJob(
    PersonalDataDownloader downloader,
    AdminDbContext dbContext,
    IStorageManager<StorageTemp> tempStorage,
    ILogger<CollectPersonalDataJob> logger)
    : Job<CollectPersonalDataJob.Data>
{
    private static readonly TimeSpan DumpPersistenceTime = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(Data data, CancellationToken cancel)
    {
        var user = data.UserId;
        var timeStart = DateTime.UtcNow;

        logger.LogInformation("Starting personal data collection for user {User}", user);

        using var writeInto = CreateTempFile();
        await downloader.CollectPersonalData(user, writeInto, cancel);

        var fileName = GenerateDumpFileName(user);
        logger.LogDebug("Writing personal data to temp file {TempFileName}", fileName);

        // If we got this far, don't cancel.
        await tempStorage.CreateFileAsync(fileName, writeInto, cancel: CancellationToken.None);

        var timeNow = DateTime.UtcNow;
        var duration = timeNow - timeStart;
        logger.LogInformation("Finished personal data collection for user {User} in {Duration}", user, duration);

        dbContext.CollectedPersonalData.Add(new CollectedPersonalData
        {
            StartedOn = timeStart,
            FinishedOn = timeNow,
            ExpiresOn = timeNow + DumpPersistenceTime,
            FileName = fileName,
            Size = writeInto.Length,
            UserId = user,
        });

        await dbContext.SaveChangesAsync(cancellationToken: CancellationToken.None);
    }

    private static string GenerateDumpFileName(Guid user)
    {
        return $"PersonalData_{user}_{RandomNumberGenerator.GetHexString(8)}.zip";
    }

    private static FileStream CreateTempFile()
    {
        return new FileStream(
            Path.GetTempFileName(),
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.None,
            4096,
            FileOptions.DeleteOnClose);
    }

    public sealed record Data(Guid UserId);
}
