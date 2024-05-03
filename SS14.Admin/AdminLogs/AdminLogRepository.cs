using System.Data;
using System.Globalization;
using System.Text.Json;
using Content.Server.Database;
using Content.Shared.Database;
using Dapper;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Models;
using SS14.Admin.Pages.Logs;

namespace SS14.Admin.AdminLogs;

public static class AdminLogRepository
{

    public class WebAdminLog
    {
        public int RoundId { get; set; }
        public int Id { get; set; }
        //not used but leave it incase
        public Round Round { get; set; } = default!;
        public LogType Type { get; set; }
        public LogImpact Impact { get; set; }
        public DateTime Date { get; set; }
        public string Message { get; set; } = default!;
        public JsonDocument Json { get; set; } = default!;
        public string? ServerName { get; set; }
    }
    public static async Task<List<WebAdminLog>> FindAdminLogs(
    DapperDBContext context,
    string? playerUserId,
    DateTime? fromDate, DateTime? toDate,
    string? serverName,
    LogType? type,
    string? search,
    int? roundId,
    int? severity,
    LogsIndexModel.OrderColumn sort,
    int limit,
    int offset
)
{
    var fromDateValue = fromDate?.ToString("o", CultureInfo.InvariantCulture);
    var toDateValue = toDate?.AddHours(23).ToString("o", CultureInfo.InvariantCulture);
    var typeInt = type.HasValue ? Convert.ToInt32(type).ToString() : null;

    var parameters = new DynamicParameters();
    parameters.Add("@FromDateValue", fromDateValue, DbType.String);
    parameters.Add("@ToDateValue", toDateValue, DbType.String);
    parameters.Add("@PlayerUserId", playerUserId != null ? (object)Guid.Parse(playerUserId) : DBNull.Value, DbType.Guid);
    parameters.Add("@ServerName", serverName, DbType.String);
    parameters.Add("@TypeInt", typeInt, DbType.String);
    parameters.Add("@Search", search, DbType.String);
    parameters.Add("@RoundId", roundId, DbType.Int32);
    parameters.Add("@Severity", severity, DbType.Int32);
    parameters.Add("@Limit", limit, DbType.Int32);
    parameters.Add("@Offset", offset, DbType.Int32);

    var sortStatement = sort switch
    {
        LogsIndexModel.OrderColumn.Date => "a.date",
        LogsIndexModel.OrderColumn.Impact => "a.impact",
        _ => throw new ArgumentOutOfRangeException(nameof(sort), sort, "Unknown admin log sort column")
    };

    var query = $@"
        SELECT a.admin_log_id AS Id, a.date, a.impact, a.json, a.message, a.type, r.round_id AS RoundId, s.server_id, s.name AS ServerName
        FROM admin_log AS a
        INNER JOIN round AS r ON a.round_id = r.round_id
        INNER JOIN server AS s ON r.server_id = s.server_id
        {(playerUserId != null ? "INNER JOIN admin_log_player AS p ON p.log_id = a.admin_log_id" : "")}
        WHERE
            {(fromDateValue != null && toDateValue != null ? "a.date BETWEEN @FromDateValue::timestamp with time zone AND @ToDateValue::timestamp with time zone AND" : "")}
            {(playerUserId != null ? "p.player_user_id = @PlayerUserId AND" : "")}
            {(serverName != null ? "s.name = @ServerName AND" : "")}
            {(typeInt != null ? "a.type = @TypeInt::integer AND" : "")}
            {(search != null ? "to_tsvector('english'::regconfig, a.message) @@ websearch_to_tsquery('english'::regconfig, @Search) AND" : "")}
            {(roundId != null ? "r.round_id = @RoundId::integer AND" : "")}
            {(severity != null ? "a.impact = @Severity::integer AND" : "")}
            TRUE
        ORDER BY {sortStatement} DESC
        LIMIT @Limit::bigint OFFSET @Offset::bigint
    ";

    using (var connection = context.Database.GetDbConnection())
    {
        await connection.OpenAsync();

        var tempResult = await connection.QueryAsync<WebAdminLog, string, JsonDocument, WebAdminLog>(
            query,
            (webAdminLog, ServerName, json) =>
            {
                webAdminLog.ServerName = ServerName;
                using (json)
                {
                    webAdminLog.Message = ProcessJsonDocument(json);
                }
                return webAdminLog;
            },
            splitOn: "ServerName,Json",
            param: parameters
        );

        return tempResult.ToList();
    }
}
    private static string ProcessJsonDocument(JsonDocument json)
    {
        return JsonSerializer.Serialize(json);
    }


    public static async Task<Player?> FindPlayerByName(DbSet<Player> players, string name)
    {
        return await players.FromSqlRaw("SELECT * FROM player WHERE last_seen_user_name = {0}", name).SingleOrDefaultAsync();
    }

}
