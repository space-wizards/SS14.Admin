using System.Text;
using Content.Server.Database;
using Content.Shared.Database;
using Microsoft.EntityFrameworkCore;

namespace SS14.Admin.Helpers;

public static class AuditHelper
{
    public static async Task AddUnsavedLogAsync(ServerDbContext db, AuditLogType ty, LogImpact impact, Guid? author, string message, List<Guid>? effected = null)
    {
            var dbEffected= effected == null
                ? []
                : effected.Select(id => new AuditLogEffectedPlayer() { EffectedUserId = id }).ToList();
            var log = new AuditLog()
            {
                Type = ty,
                Impact = impact,
                AuthorUserId = author,
                Message = message,
                Date = DateTime.UtcNow,
                Effected = dbEffected,
            };
            db.AuditLog.Add(log);
    }

    public static async Task<string> GetNameFromUidOrDefault(ServerDbContext db, Guid? author)
    {
        if (author == null)
            return "System";

        return (await db.Player.FirstOrDefaultAsync(p => p.UserId == author))?.LastSeenUserName ?? "System";
    }

    // I know this takes 1000 parameters, but this is needed for a consistent format for reachability.
    public static async Task UnsavedLogForAddRemarkAsync(ServerDbContext db, NoteType type, int noteId, bool secret, Guid? authorUid, string message, DateTime? expiryTime, Guid? target, NoteSeverity? severity = null)
    {
        var authorName = await GetNameFromUidOrDefault(db, authorUid);
        var sb = new StringBuilder($"{authorName} added");

        if (secret && type == NoteType.Note)
        {
            sb.Append(" secret");
        }

        sb.Append($" {type} {noteId} with message \"{message}\"");

        switch (type)
        {
            case NoteType.Note:
                sb.Append($" with {severity} severity");
                break;
            case NoteType.Message:
                break;
            case NoteType.Watchlist:
                break;
            case NoteType.ServerBan:
                break;
            case NoteType.RoleBan:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown note type");
        }

        if (expiryTime is not null)
        {
            sb.Append($" which expires on {expiryTime.Value.ToUniversalTime(): yyyy-MM-dd HH:mm:ss} UTC");
        }

        await AddUnsavedLogAsync(
            db, AuditLogUtil.LogTypeForRemark(type), AuditLogUtil.LogImpactForRemark(type, severity),
            authorUid, sb.ToString(), target != null ? [target.Value] : []);
    }

    public static async Task UnsavedLogForRemoveRemakAsync(ServerDbContext db, NoteType type, int noteId, Guid? remover,
        Guid? target, NoteSeverity? severity = null)
    {
        var removerName = await GetNameFromUidOrDefault(db, remover);
        var logStr = $"{removerName} has deleted {type} {noteId}";
        await AddUnsavedLogAsync(db, AuditLogUtil.LogTypeForRemark(type),
            AuditLogUtil.LogImpactForRemark(type, severity),
            remover, logStr, target != null ? [target.Value] : []);
    }
}
