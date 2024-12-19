using Microsoft.Extensions.Options;

namespace SS14.Admin.JobSystem;

public sealed class IntervalJobService(
    IJobScheduler jobScheduler,
    TimeProvider timeProvider,
    IOptions<JobSystemConfiguration> options,
    ILogger<IntervalJobService> logger)
    : BackgroundService
{
    private readonly List<JobDatum> _jobs = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        InitializeJobs();

        InitializeJobNextTimes();

        while (!stoppingToken.IsCancellationRequested)
        {
            var waitTime = SortAndGetNextWaitTime();

            logger.LogTrace("Sleeping for {SleepTime} until ", waitTime);

            await Task.Delay(waitTime, timeProvider, stoppingToken);

            var timeNow = timeProvider.GetTimestamp();

            foreach (var job in _jobs)
            {
                if (job.NextFireTime < timeNow)
                {
                    FireJob(job);

                    job.NextFireTime = OffsetTimestamp(timeProvider, timeNow, job.Definition.Interval);
                }
                else
                {
                    break;
                }
            }
        }
    }

    private async void FireJob(JobDatum job)
    {
        try
        {
            await jobScheduler.QueueJobAsync(job.Definition.Key, JobData.Empty);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to fire job");
        }
    }

    private void InitializeJobs()
    {
        foreach (var job in options.Value.IntervalJobs)
        {
            _jobs.Add(new JobDatum
            {
                Definition = job,
                NextFireTime = 0,
            });
        }
    }

    private void InitializeJobNextTimes()
    {
        var now = timeProvider.GetTimestamp();

        foreach (var job in _jobs)
        {
            if (job.Definition.OnStartup)
            {
                job.NextFireTime = 0;
            }
            else
            {
                job.NextFireTime = OffsetTimestamp(timeProvider, now, job.Definition.Interval);
            }
        }
    }

    private static long OffsetTimestamp(TimeProvider timeProvider, long timestamp, TimeSpan amount)
    {
        return timestamp + (long)(amount.Ticks * (timeProvider.TimestampFrequency / (double)TimeSpan.TicksPerSecond));
    }

    private TimeSpan SortAndGetNextWaitTime()
    {
        _jobs.Sort(JobDatum.NextFireTimeComparer.Instance);

        if (_jobs.Count == 0)
            return TimeSpan.MaxValue;

        var timeStampNow = timeProvider.GetTimestamp();

        var closestJob = _jobs[0];
        var waitTime = timeProvider.GetElapsedTime(timeStampNow, closestJob.NextFireTime);

        return waitTime < TimeSpan.Zero ? TimeSpan.Zero : waitTime;
    }

    private sealed class JobDatum
    {
        public required JobSystemConfiguration.IntervalJob Definition;
        public required long NextFireTime;

        public sealed class NextFireTimeComparer : IComparer<JobDatum>
        {
            public static readonly NextFireTimeComparer Instance = new();

            public int Compare(JobDatum? x, JobDatum? y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (y is null) return 1;
                if (x is null) return -1;
                return x.NextFireTime.CompareTo(y.NextFireTime);
            }
        }
    }
}
