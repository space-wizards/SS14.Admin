using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SS14.Admin.Data;

public sealed class AdminDbContext(DbContextOptions<AdminDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CollectedPersonalData>()
            .HasIndex(p => p.UserId);
    }

    public required DbSet<CollectedPersonalData> CollectedPersonalData { get; init; }
    // Job storage not implemented right now, don't care.
    // public required DbSet<Job> Job { get; init; }
}

public sealed class CollectedPersonalData
{
    public int Id { get; set; }
    public Guid UserId { get; set; }

    public DateTime StartedOn { get; set; }
    public DateTime FinishedOn { get; set; }
    public DateTime ExpiresOn { get; set; }

    [MaxLength(128)]
    public required string FileName { get; set; }
    public long Size { get; set; }
}

// public sealed class Job
// {
//     public int Id { get; set; }
//     public required string Key { get; set; }
//     public DateTime JobAdded { get; set; }
//
//     [DataType("jsonb")]
//     public string? Data { get; set; }
// }
