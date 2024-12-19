using Microsoft.EntityFrameworkCore;
using SS14.Admin.Data;
using SS14.Admin.JobSystem;
using SS14.Admin.Storage;
using Job = SS14.Admin.JobSystem.Job;

namespace SS14.Admin.PersonalData;

[Job("PersonalData.DeleteCollected")]
public sealed class DeleteCollectedPersonalDataJob(
    AdminDbContext dbContext,
    ITempStorageManager tempStorage,
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
            var path = tempStorage.GetFilePath(item.FileName);
            logger.LogTrace("Deleting {File}", path);

            File.Delete(path);

            dbContext.CollectedPersonalData.Remove(item);
        }

        await dbContext.SaveChangesAsync();

        logger.LogDebug("Deleted {DeletedCount} personal data collections", toDelete.Count);
    }
}
