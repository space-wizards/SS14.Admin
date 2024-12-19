namespace SS14.Admin.JobSystem;

public interface IJobScheduler
{
    Task QueueJobAsync(string key, JobData data, JobOptions? options = null);
}

public static class JobSchedulerExtensions
{
    public static async Task QueueJobAsync<TJob, TData>(this IJobScheduler scheduler, TData data, JobOptions? options = null)
        where TJob : Job<TData>
        where TData : notnull
    {
        await scheduler.QueueJobAsync(
            JobInternal.GetJobKey(typeof(TJob)),
            JobData.FromObject(data),
            options);
    }

    public static async Task QueueJobAsync<TJob>(this IJobScheduler scheduler, JobOptions? options = null)
        where TJob : Job
    {
        await scheduler.QueueJobAsync(
            JobInternal.GetJobKey(typeof(TJob)),
            JobData.Empty,
            options);
    }
}
