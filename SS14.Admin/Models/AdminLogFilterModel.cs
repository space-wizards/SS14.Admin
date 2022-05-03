using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SS14.Admin.AdminLogs;

namespace SS14.Admin.Models
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class AdminLogFilterModel
    {
        [Required, JsonInclude,JsonConverter(typeof(JsonStringEnumConverter))] public LogFilterTags Key;
        [Required, JsonInclude] public string Value = default!;
    }
}
