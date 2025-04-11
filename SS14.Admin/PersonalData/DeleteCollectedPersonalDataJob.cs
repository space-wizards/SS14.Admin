using Microsoft.EntityFrameworkCore;
using SS14.Admin.Data;
using SS14.Admin.JobSystem;
using SS14.Admin.Storage;
using Job = SS14.Admin.JobSystem.Job;

namespace SS14.Admin.PersonalData;

[Job("PersonalData.DeleteCollected")]
public sealed class DeleteCollectedPersonalDataJob(
    AdminDbContext dbContext,
    IStorageManager<StorageTemp> tempStorage,
    ILogger<DeleteCollectedPersonalDataJob> logger) : Job
{
    protected override async Task ExecuteAsync(CancellationToken cancel)
    {
        logger.LogTrace("Deleting expired personal data collections...");

        var toDelete = await dbContext.CollectedPersonalData
            .Where(p => p.ExpiresOn < DateTime.UtcNow)
            .ToListAsync(cancel);

        foreach (var item in toDelete)
        {
            logger.LogTrace("Deleting {File}", item.FileName);

            await tempStorage.DeleteFileAsync(item.FileName, cancel: CancellationToken.None);

            dbContext.CollectedPersonalData.Remove(item);
        }

        await dbContext.SaveChangesAsync(cancellationToken: CancellationToken.None);

        logger.LogDebug("Deleted {DeletedCount} personal data collections", toDelete.Count);
    }
}
