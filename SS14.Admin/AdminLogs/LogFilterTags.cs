using System;
using Content.Server.Database;
using Content.Shared.Database;

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
        public static string? TransformValue(this LogFilterTags tag, ServerDbContext context, string value)
        {
            return tag switch
            {
                LogFilterTags.Player => AdminLogRepository.FindPlayerByName(context.Player, value).Result?.UserId.ToString(),
                LogFilterTags.Type => Enum.TryParse(value, out LogType type) ? Convert.ToInt32(type).ToString() : default,
                LogFilterTags.Server => value,
                LogFilterTags.Search => value,
                _ => throw new ArgumentOutOfRangeException(nameof(tag), tag, null)
            };
        }

        private static string TextSearchForContext(ServerDbContext context)
        {
            return context is PostgresServerDbContext ? "to_tsvector('english'::regconfig, a.message) @@ websearch_to_tsquery('english'::regconfig, #)" : " a.message LIKE %#%";
        }
    }
}
