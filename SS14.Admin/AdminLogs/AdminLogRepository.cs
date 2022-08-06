
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Models;

namespace SS14.Admin.AdminLogs;

public static class AdminLogRepository
{
    private const string LogBaseQuery = @"
        SELECT a.admin_log_id, a.date, a.impact, a.json, a.message, a.type, r.round_id, s.server_id, s.name FROM admin_log AS a
            INNER JOIN round AS r ON a.round_id = r.round_id
            INNER JOIN server AS s ON r.server_id = s.server_id
    ";

    private const string PlayerQuery = @"
        SELECT * FROM player WHERE last_seen_user_name = {0}
    ";

    private const string PlayerJoinStatement = "INNER JOIN admin_log_player AS p ON p.log_id = a.admin_log_id";
    private const string WhereStatementStart = " WHERE";
    private const string DateSortStatement = " ORDER BY a.date DESC";
    private const string PaginationStatement = " LIMIT # OFFSET #";
    private const string DateStatement = " a.date BETWEEN #::timestamp with time zone AND #::timestamp with time zone";

    private static readonly Regex ParameterRegex = new(Regex.Escape("#"));

    public static List<AdminLog> List(DbSet<AdminLog> adminLogs, int limit = 100, int offset = 0)
    {
        var query = CreateLogQuery();
        query.Append(DateSortStatement);
        return Complete(adminLogs, query, limit, offset);
    }

    public static List<AdminLog> List(ServerDbContext context, DbSet<AdminLog> adminLogs, List<AdminLogFilterModel> filters,
        DateTime? fromDate, DateTime? toDate, int limit = 100, int offset = 0)
    {
        var query = CreateLogQuery();

        var values = new List<string>();

        if (filters.Find(filter => filter.Key == LogFilterTags.Player) != default)
            query.Append(PlayerJoinStatement);

        query.Append(WhereStatementStart);

        if (fromDate.HasValue && toDate.HasValue)
        {
            values.Add(fromDate.Value.ToString("o", CultureInfo.InvariantCulture));
            values.Add(toDate.Value.AddHours(23).ToString("o", CultureInfo.InvariantCulture));
            query.Append(DateStatement);

            if (filters.Count > 0) query.Append(" AND ");
        }

        var first = true;
        foreach (var filter in filters)
        {
            var value = filter.Key.TransformValue(context, filter.Value);
            if (value == null) throw new ArgumentException($"Failed to parse filter value: {filter.Value}");

            if (!first) query.Append(" AND ");
            values.Add(value);
            query = Filter(context, query, filter.Key);
            first = false;
        }

        query.Append(DateSortStatement);
        return Complete(adminLogs, query, limit, offset, values);
    }

    public static Player? FindPlayerByName(DbSet<Player> players, string name)
    {
        return players.FromSqlRaw(PlayerQuery, name).SingleOrDefault();
    }

    private static StringBuilder CreateLogQuery()
    {
        return new StringBuilder(LogBaseQuery);
    }

    private static StringBuilder Filter(ServerDbContext context, StringBuilder query, LogFilterTags filter)
    {
        return query.Append(filter.FilterQueryPart(context));
    }

    private static List<AdminLog> Complete(DbSet<AdminLog> adminLogs, StringBuilder query, int limit, int offset,  IEnumerable<string>? parameters = null)
    {
        var parameterList = new List<object>(parameters ?? Array.Empty<string>())
        {
            limit,
            offset
        };

        query = query.Append(PaginationStatement);

        var querystring = EnumerateParameters(query.ToString());

        var processedQuery = adminLogs.FromSqlRaw(querystring, parameterList.ToArray());
        return processedQuery.ToList();
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
}
