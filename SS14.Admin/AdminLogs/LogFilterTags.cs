using System;
using System.Linq;
using Content.Server.Database;
using Content.Shared.Database;
using Microsoft.EntityFrameworkCore;

namespace SS14.Admin.AdminLogs
{
    public enum LogFilterTags
    {
        Search,
        Type,
        Server,
        Player
    }

    public static class LogFilterTagsExtension
    {
        public static IQueryable<AdminLog> FilterQueryPart(this LogFilterTags tag, ServerDbContext context, string value, IQueryable<AdminLog> query)
        {
            LogType type;
            return tag switch
            {
                LogFilterTags.Player => query.Where(log => log.Players.Any(player => EF.Functions.ILike(player.Player.LastSeenUserName, "%" + value + "%"))), //TODO: Implement player query
                LogFilterTags.Server => query.Where(log => EF.Functions.ILike(log.Round!.Server!.Name, "%" + value + "%")),
                LogFilterTags.Type => query.Where(log => Enum.TryParse(value, true, out type) && log.Type == type),
                LogFilterTags.Search => context.SearchLogs(query, value),
                _ => throw new ArgumentOutOfRangeException(nameof(tag), tag, null)
            };
        }
    }
}
