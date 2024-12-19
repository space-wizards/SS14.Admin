using Microsoft.Extensions.Options;

namespace SS14.Admin.JobSystem;

public sealed class JobSchedulerService : IHostedService, IJobScheduler
{
    private readonly JobSystemConfiguration _configuration;
    private readonly ILogger<JobSchedulerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, JobSystemConfiguration.JobRegistration> _registrations = new();

    private readonly List<JobDatum> _runningJobs = [];
    private readonly object _jobLock = new();

    public JobSchedulerService(IOptions<JobSystemConfiguration> options, ILogger<JobSchedulerService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = options.Value;

        foreach (var registration in _configuration.Jobs)
        {
            _registrations.Add(registration.Key, registration);
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cancelling running jobs...");

        JobDatum[] jobsCopy;
        lock (_jobLock)
        {
            jobsCopy = _runningJobs.ToArray();
        }

        var tasks = jobsCopy.Select(j => j.Task).ToArray();
        foreach (var runningJob in jobsCopy)
        {
            await runningJob.CancellationTokenSource.CancelAsync();
        }

        await Task.WhenAll(tasks).WaitAsync(cancellationToken);
    }

    public Task QueueJobAsync(string key, JobData data, JobOptions? options = null)
    {
        var registration = _registrations[key];
        _logger.LogDebug("Queuing job: {JobKey}", key);

        lock (_runningJobs)
        {
            var datum = new JobDatum
            {
                CancellationTokenSource = new CancellationTokenSource(),
                Registration = registration,
                Data = data,
            };

            var task = Task.Run(async () =>
            {
                await RunJob(datum, datum.CancellationTokenSource.Token);
            });

            datum.Task = task;
            _runningJobs.Add(datum);
        }

        return Task.CompletedTask;
    }

    private async Task RunJob(JobDatum datum, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();

            var job = (IJob)ActivatorUtilities.CreateInstance(scope.ServiceProvider, datum.Registration.Type);

            await job.ExecuteAsync(datum.Data, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error running job: {JobKey}", datum.Registration.Key);
        }
    }

    private sealed class JobDatum
    {
        public required CancellationTokenSource CancellationTokenSource;
        public required JobSystemConfiguration.JobRegistration Registration;
        public required JobData Data;
        public Task Task = default!;
    }
}
