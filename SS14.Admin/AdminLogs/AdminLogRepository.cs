using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Content.Server.Database;
using Content.Shared.Database;
using Dapper;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Pages.Logs;

namespace SS14.Admin.AdminLogs;

public static class AdminLogRepository
{
    private static readonly Regex ParameterRegex = new(Regex.Escape("#"));

    public class WebAdminLog : AdminLog
    {
        public string ServerName { get; set; }
    }
    public static async Task<List<WebAdminLog>> FindAdminLogs(
        ServerDbContext context,
        DbSet<AdminLog> adminLogs,
        string? playerUserId,
        DateTime? fromDate, DateTime? toDate,
        string? serverName,
        LogType? type,
        string? search,
        int? roundId,
        int? severity,
        LogsIndexModel.OrderColumn sort,
        int limit = 100, int offset = 0)
    {
        var fromDateValue = fromDate?.ToString("o", CultureInfo.InvariantCulture);
        var toDateValue = toDate?.AddHours(23).ToString("o", CultureInfo.InvariantCulture);
        var typeInt = type.HasValue ? Convert.ToInt32(type).ToString() : null;

        var values = new List<object>();
        if (fromDateValue != null && toDateValue != null)
        {
            values.Add(fromDateValue);
            values.Add(toDateValue);
        }

        if (playerUserId != null) values.Add(Guid.Parse(playerUserId));
        if (serverName != null) values.Add(serverName);
        if (typeInt != null) values.Add(typeInt);
        if (search != null) values.Add(search);
        if (roundId.HasValue) values.Add(roundId);
        if (severity != null) values.Add(severity);


        values.Add(limit.ToString());
        values.Add(offset.ToString());

        var sortStatement = sort switch
        {
            LogsIndexModel.OrderColumn.Date => "a.date",
            LogsIndexModel.OrderColumn.Impact => "a.impact",
            _ => throw new ArgumentOutOfRangeException(nameof(sort), sort, "Unknown admin log sort column")
        };

        var query = $@"
        SELECT a.admin_log_id, a.date, a.impact, a.json, a.message, a.type, r.round_id, s.server_id, s.name AS ServerName FROM admin_log AS a
            INNER JOIN round AS r ON a.round_id = r.round_id
            INNER JOIN server AS s ON r.server_id = s.server_id
            {(playerUserId != null ? "INNER JOIN admin_log_player AS p ON p.log_id = a.admin_log_id" : "")}
            WHERE
                {(fromDateValue != null && toDateValue != null ? "a.date BETWEEN '#'::timestamp with time zone AND '#'::timestamp with time zone AND" : "")}
                {(playerUserId != null ? "p.player_user_id = # AND" : "")}
                {(serverName != null ? "s.name = # AND" : "")}
                {(typeInt != null ? "a.type = #::integer AND" : "")}
                {(search != null ? $"{TextSearchForContext(context)} AND" : "")}
                {(roundId != null ? "r.round_id = #::integer AND" : "")}
                {(severity != null ? "a.impact = #::integer AND" : "")}
                TRUE
            ORDER BY {sortStatement} DESC
            LIMIT #::bigint OFFSET #::bigint
        ";

        Console.Write(values.ToString());
        Console.Write(query);
        var finalQuery = CreateSqlFromParameter(query, values);
        Console.Write(finalQuery);

        using (var connection = context.Database.GetDbConnection())
        {
            connection.Open();

            var result = await connection.QueryAsync<WebAdminLog, string, WebAdminLog>(
                finalQuery,
                (log, json) =>
                {
                    log.Json = JsonDocument.Parse(json);
                    return log;
                },
                splitOn: "Json"
            );

            return result.ToList();
        }
    }

    public static async Task<Player?> FindPlayerByName(DbSet<Player> players, string name)
    {
        return await players.FromSqlRaw("SELECT * FROM player WHERE last_seen_user_name = {0}", name).SingleOrDefaultAsync();
    }

    private static string TextSearchForContext(ServerDbContext context)
    {
        return context is PostgresServerDbContext ? "to_tsvector('english'::regconfig, a.message) @@ websearch_to_tsquery('english'::regconfig, #)" : " a.message LIKE %#%";
    }

    private static string CreateSqlFromParameter(string query, List<object> parameters)
    {
        return SetParameters(EnumerateParameters(query), parameters);
    }
    private static string EnumerateParameters(string query)
    {
        var index = 0;

        while (ParameterRegex.IsMatch(query))
        {
            query = ParameterRegex.Replace(query, $"{{{index}}}", 1);
            index += 1;
        }

        return query;
    }

    public static string SetParameters(string query, List<object> values)
    {
        var index = 0;

            while (index < values.Count)
            {
                var paramValue = values[index];
                var paramPlaceholder = $"{{{index}}}";
                query = query.Replace(paramPlaceholder, paramValue?.ToString() ?? "NULL");
                index += 1;
            }

        return query;
    }
}
