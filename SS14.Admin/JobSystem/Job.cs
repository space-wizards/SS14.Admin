using System.Reflection;
using System.Text.Json;
using JetBrains.Annotations;

namespace SS14.Admin.JobSystem;

public interface IJob
{
    Task ExecuteAsync(JobData data, CancellationToken cancel);
}

public abstract class Job : IJob
{
    protected abstract Task ExecuteAsync(CancellationToken cancel);

    Task IJob.ExecuteAsync(JobData data, CancellationToken cancel)
    {
        return ExecuteAsync(cancel);
    }
}

public abstract class Job<T> : IJob where T : notnull
{
    protected abstract Task ExecuteAsync(T data, CancellationToken cancel);

    Task IJob.ExecuteAsync(JobData data, CancellationToken cancel)
    {
        var deserialized = JsonSerializer.Deserialize<T>(data.Data, JobInternal.JsonOptions);
        return ExecuteAsync(deserialized!, cancel);
    }
}

public sealed class JobData
{
    public static readonly JobData Empty = new JobData { Data = [] };

    public required byte[] Data { get; init; }

    public static JobData FromObject(object obj)
    {
        return new JobData
        {
            Data = JsonSerializer.SerializeToUtf8Bytes(obj, JobInternal.JsonOptions),
        };
    }
}

public sealed class JobOptions
{
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[BaseTypeRequired(typeof(IJob))]
public sealed class JobAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

internal static class JobInternal
{
    internal static readonly JsonSerializerOptions JsonOptions = new();

    private static readonly Dictionary<Type, string> JobKeys = new();

    public static string GetJobKey(Type type)
    {
        lock (JobKeys)
        {
            if (!JobKeys.TryGetValue(type, out var key))
            {
                key = CalcJobKey(type);
                JobKeys[type] = key;
            }

            return key;
        }
    }

    private static string CalcJobKey(Type type)
    {
        var attr = type.GetCustomAttribute<JobAttribute>();
        if (attr is null)
            throw new ArgumentException("Type is missing JobAttribute");

        return attr.Name;
    }
}
