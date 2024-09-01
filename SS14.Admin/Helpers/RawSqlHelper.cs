using System.Text.RegularExpressions;
using Content.Server.Database;

namespace SS14.Admin.Helpers;

/// <summary>
/// Misc utilities for working with raw SQL
/// </summary>
public static class RawSqlHelper
{
    private static readonly Regex ParameterRegex = new(Regex.Escape("#"));

    public static string TextSearchForContext(ServerDbContext context)
    {
        return context is PostgresServerDbContext ? "to_tsvector('english'::regconfig, a.message) @@ websearch_to_tsquery('english'::regconfig, #)" : " a.message LIKE %#%";
    }

    /// <summary>
    /// Replace "#" with the next parameter number. Used for when you have a variable ammount of parameters.
    /// </summary>
    public static string EnumerateParameters(string query)
    {
        var index = 0;

        while (ParameterRegex.IsMatch(query))
        {
            query = ParameterRegex.Replace(query, $"{{{index}}}", 1);
            index += 1;
        }

        return query;
    }
}
