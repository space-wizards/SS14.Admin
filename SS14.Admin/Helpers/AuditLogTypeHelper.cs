using System.Text.Json;
using Content.Shared.Database;

namespace SS14.Admin.Helpers;

internal static class AuditLogTypeHelper
{
    public static string AuditLogTypeJson { get;  }

    static AuditLogTypeHelper()
    {
        var dict = new Dictionary<string, int>();
        foreach (var type in Enum.GetNames<AuditLogType>())
        {
            dict[type] = (int)Enum.Parse<AuditLogType>(type);
        }

        AuditLogTypeJson = JsonSerializer.Serialize(dict);
    }
}
