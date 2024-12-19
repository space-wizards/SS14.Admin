namespace SS14.Admin.JobSystem;

public sealed class JobSystemConfiguration
{
    internal readonly List<IntervalJob> IntervalJobs = [];
    internal readonly List<JobRegistration> Jobs = [];

    public void RegisterIntervalJob<TJob>(TimeSpan interval, bool onStartup = true) where TJob : Job
    {
        var key = JobInternal.GetJobKey(typeof(TJob));

        IntervalJobs.Add(new IntervalJob
        {
            Key = key,
            Interval = interval,
            OnStartup = onStartup,
        });

        RegisterJob<TJob>();
    }

    public void RegisterJob<TJob>() where TJob : IJob
    {
        var key = JobInternal.GetJobKey(typeof(TJob));

        Jobs.Add(new JobRegistration
        {
            Key = key,
            Type = typeof(TJob),
        });
    }

    public sealed class IntervalJob
    {
        public required string Key;
        public required TimeSpan Interval;
        public required bool OnStartup;
    }

    public sealed class JobRegistration
    {
        public required string Key;
        public required Type Type;
    }
}
