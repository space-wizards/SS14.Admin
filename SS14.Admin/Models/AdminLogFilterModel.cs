using System.ComponentModel.DataAnnotations;

namespace SS14.Admin.Models
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public record AdminLogFilterModel([Required] string Key, [Required] string Value);
}
