using System.Globalization;
using Content.Server.Database;
using Content.Shared.Database;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Helpers;

namespace SS14.Admin.AuditLogs;

public static class AuditLogRepository
{
    public static async Task<List<AuditLog>> FindAuditLogs(
        ServerDbContext context,
        Guid? authorUserId,
        Guid? effectedUserId,
        DateTime? fromDate, DateTime? toDate,
        AuditLogType? type,
        string? textSearch,
        int? severity,
        int limit = 100, int offset = 0)
    {
        var fromDateValue = fromDate?.ToString("o", CultureInfo.InvariantCulture);
        var toDateValue = toDate?.AddHours(23).ToString("o", CultureInfo.InvariantCulture);
        uint? typeInt = type.HasValue ? Convert.ToUInt32(type) : null;

        var values = new List<object>();
        if (fromDateValue != null && toDateValue != null)
        {
            values.Add(fromDateValue);
            values.Add(toDateValue);
        }

        if (authorUserId != null) values.Add(authorUserId);
        if (effectedUserId != null) values.Add(effectedUserId);
        if (typeInt != null) values.Add(typeInt);
        if (textSearch != null) values.Add(textSearch);
        if (severity != null) values.Add(severity);

        values.Add(limit.ToString());
        values.Add(offset.ToString());

        var query = $@"
            SELECT a.audit_log_id, a.type, a.impact, a.date, a.message, a.author_user_id FROM audit_log AS a
                {(effectedUserId != null ? "INNER JOIN audit_log_effected_player AS p ON p.audit_log_id = a.audit_log_id" : "")}
            WHERE
                {(fromDateValue != null && toDateValue != null ? "a.date BETWEEN #::timestamp with time zone AND #::timestamp with time zone AND" : "")}
                {(authorUserId != null ? "a.author_user_id = # AND" : "")}
                {(effectedUserId != null ? "p.effected_user_id = # AND" : "")}
                {(typeInt != null ? "a.type = #::integer AND" : "")}
                {(textSearch != null ? $"{RawSqlHelper.TextSearchForContext(context)} AND" : "")}
                {(severity != null ? "a.impact = #::integer AND" : "")}
                TRUE
            ORDER BY a.date DESC
            LIMIT #::bigint OFFSET #::bigint
        ";

        var result = context.AuditLog.FromSqlRaw(RawSqlHelper.EnumerateParameters(query), values.ToArray());
        var logs =  await result.ToListAsync();
        foreach (var log in logs)
        {
            //log.Effected = await context.AuditLogEffectedPlayer.FromSqlRaw($"SELECT a.audit_log_effected_players_id, a.audit_log_id, a.effected_user_id FROM audit_log_effected_player AS a WHERE a.audit_log_id = {{0}}", log.Id).ToListAsync();
            log.Effected = await context.AuditLogEffectedPlayer.Where(p => p.AuditLogId == log.Id).ToListAsync();
        }
        return logs;
    }
}
