using System.Text.Json;
using Content.Shared.Database;

namespace SS14.Admin.Helpers;

internal static class AdminLogTypeHelper
{
    public static string AdminLogTypeJson { get;  }

    static AdminLogTypeHelper()
    {
        var dict = new Dictionary<string, int>();
        foreach (var type in Enum.GetNames<LogType>())
        {
            dict[type] = (int)Enum.Parse<LogType>(type);
        }

        AdminLogTypeJson = JsonSerializer.Serialize(dict);
    }
}
