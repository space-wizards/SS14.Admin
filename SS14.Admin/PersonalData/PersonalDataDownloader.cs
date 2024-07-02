using System.IO.Compression;
using Content.Server.Database;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace SS14.Admin.PersonalData;

public sealed class PersonalDataDownloader(PostgresServerDbContext dbContext)
{
    private const string SupportedDbMigration = "20240623005121_BanTemplate";

    public async Task<bool> GetIsMigrationCompatible(CancellationToken cancel = default)
    {
        var migrations = await dbContext.Database.GetAppliedMigrationsAsync(cancel);
        return !migrations.Any(m => StringComparer.Ordinal.Compare(m, SupportedDbMigration) > 0);
    }

    public async Task CollectPersonalData(Guid userId, Stream into, CancellationToken cancel = default)
    {
        using var zip = new ZipArchive(into, ZipArchiveMode.Create, leaveOpen: true);

        await CollectAdmin(userId, zip, cancel);
        await CollectAdminLogs(userId, zip, cancel);
        await CollectAdminMessages(userId, zip, cancel);
        await CollectAdminNotes(userId, zip, cancel);
        await CollectAdminWatchlists(userId, zip, cancel);
        await CollectConnectionLog(userId, zip, cancel);
        await CollectPlayer(userId, zip, cancel);
        await CollectPlayTime(userId, zip, cancel);
        await CollectPreference(userId, zip, cancel);
        await CollectServerBan(userId, zip, cancel);
        await CollectServerBanExemption(userId, zip, cancel);
        await CollectServerRoleBan(userId, zip, cancel);
        await CollectUploadedResourceLog(userId, zip, cancel);
        await CollectWhitelist(userId, zip, cancel);
        await CollectRoleWhitelist(userId, zip, cancel);
    }

    private async Task CollectAdmin(Guid userId, ZipArchive into, CancellationToken cancel)
    {
        await CollectJson(userId, into, "admin", """
        SELECT
            COALESCE(json_agg(to_jsonb(data) - 'admin_rank_id'), '[]') #>> '{}'
        FROM (
            SELECT
                *,
                (SELECT to_json(rank) FROM (
                    SELECT * FROM admin_rank WHERE admin_rank.admin_rank_id = admin.admin_rank_id
                ) rank)
                as admin_rank,
                (SELECT COALESCE(json_agg(to_jsonb(flagg) - 'admin_id'), '[]') FROM (
                    SELECT * FROM admin_flag WHERE admin_id = @UserId
                ) flagg)
                as admin_flags
            FROM
                admin
            WHERE
                user_id = @UserId
        ) as data
        """, cancel);
    }

    private async Task CollectAdminLogs(Guid userId, ZipArchive into, CancellationToken cancel)
    {
        await CollectJson(userId, into, "admin_log", """
        SELECT
            COALESCE(json_agg(to_jsonb(data) - 'admin_log_id'), '[]') #>> '{}'
        FROM (
            SELECT
                *
            FROM
                admin_log_player alp
            INNER JOIN
                admin_log al
            ON
                al.admin_log_id = alp.log_id AND al.round_id = alp.round_id
            WHERE
                player_user_id = @UserId
        ) as data
        """, cancel);
    }

    private async Task CollectAdminNotes(Guid userId, ZipArchive into, CancellationToken cancel)
    {
        await CollectJson(userId, into, "admin_notes", """
        SELECT
            COALESCE(json_agg(to_json(data)), '[]') #>> '{}'
        FROM (
            SELECT
                *
            FROM
                admin_notes
            WHERE
                player_user_id = @UserId
        ) as data
        """, cancel);
    }

    private async Task CollectConnectionLog(Guid userId, ZipArchive into, CancellationToken cancel)
    {
        await CollectJson(userId, into, "connection_log", """
        SELECT
            COALESCE(json_agg(to_jsonb(data)), '[]') #>> '{}'
        FROM (
            SELECT
                *,
                (SELECT COALESCE(json_agg(to_jsonb(ban_hit)), '[]') FROM (
                    SELECT * FROM server_ban_hit WHERE connection_id = connection_log_id
                ) ban_hit)
                as ban_hits
            FROM
                connection_log
            WHERE
                user_id = @UserId
        ) as data
        """, cancel);
    }

    private async Task CollectPlayTime(Guid userId, ZipArchive into, CancellationToken cancel)
    {
        await CollectJson(userId, into, "play_time", """
        SELECT
            COALESCE(json_agg(to_jsonb(data)), '[]') #>> '{}'
        FROM (
            SELECT
                *
            FROM
                play_time
            WHERE
                player_id = @UserId
        ) as data
        """, cancel);
    }

    private async Task CollectPlayer(Guid userId, ZipArchive into, CancellationToken cancel)
    {
        await CollectJson(userId, into, "player", """
        SELECT
            COALESCE(json_agg(to_jsonb(data)), '[]') #>> '{}'
        FROM (
            SELECT
                *,
                (SELECT COALESCE(json_agg(to_jsonb(player_round_subquery) - 'players_id'), '[]') FROM (
                    SELECT * FROM player_round WHERE players_id = player_id
                ) player_round_subquery)
                as player_rounds
            FROM
                player
            WHERE
                user_id = @UserId
        ) as data
        """, cancel);
    }

    private async Task CollectPreference(Guid userId, ZipArchive into, CancellationToken cancel)
    {
        await CollectJson(userId, into, "preference", """
        SELECT
            COALESCE(json_agg(to_jsonb(data)), '[]') #>> '{}'
        FROM (
            SELECT
                *,
                (SELECT json_agg(to_jsonb(profile_subq) - 'preference_id') FROM (
                    SELECT
                        *,
                        (SELECT COALESCE(json_agg(to_jsonb(job_subq) - 'profile_id'), '[]') FROM (
                            SELECT * FROM job WHERE job.profile_id = profile.profile_id
                        ) job_subq)
                        as jobs,
                        (SELECT COALESCE(json_agg(to_jsonb(antag_subq) - 'profile_id'), '[]') FROM (
                            SELECT * FROM antag WHERE antag.profile_id = profile.profile_id
                        ) antag_subq)
                        as antags,
                        (SELECT COALESCE(json_agg(to_jsonb(trait_subq) - 'profile_id'), '[]') FROM (
                            SELECT * FROM trait WHERE trait.profile_id = profile.profile_id
                        ) trait_subq)
                        as traits,
                        (SELECT COALESCE(json_agg(to_jsonb(role_loadout_subq) - 'profile_id'), '[]') FROM (
                            SELECT
                                *,
                                (SELECT COALESCE(json_agg(to_jsonb(loadout_group_subq) - 'profile_role_loadout_id'), '[]') FROM (
                                    SELECT
                                        *,
                                        (SELECT COALESCE(json_agg(to_jsonb(loadout_subq) - 'profile_loadout_group_id'), '[]') FROM (
                                            SELECT * FROM profile_loadout pl WHERE pl.profile_loadout_group_id = plg.profile_loadout_group_id
                                        ) loadout_subq)
                                        as loadouts
                                    FROM profile_loadout_group plg
                                    WHERE plg.profile_role_loadout_id = prl.profile_role_loadout_id
                                ) loadout_group_subq)
                                as loadout_groups
                            FROM profile_role_loadout prl
                            WHERE prl.profile_id = profile.profile_id
                        ) role_loadout_subq)
                        as role_loadouts
                    FROM
                        profile
                    WHERE
                        profile.preference_id = preference.preference_id
                ) profile_subq)
                as profiles
            FROM
                preference
            WHERE
                user_id = @UserId
        ) as data
        """, cancel);
    }

    private async Task CollectServerBan(Guid userId, ZipArchive into, CancellationToken cancel)
    {
        await CollectJson(userId, into, "server_ban", """
        SELECT
            COALESCE(json_agg(to_json(data)), '[]') #>> '{}'
        FROM (
            SELECT
                *,
                (SELECT to_jsonb(unban_sq) - 'ban_id' FROM (
                    SELECT * FROM server_unban WHERE server_unban.ban_id = server_ban.server_ban_id
                ) unban_sq)
                as unban
            FROM
                server_ban
            WHERE
                player_user_id = @UserId
        ) as data
        """, cancel);
    }

    private async Task CollectServerBanExemption(Guid userId, ZipArchive into, CancellationToken cancel)
    {
        await CollectJson(userId, into, "server_ban_exemption", """
        SELECT
            COALESCE(json_agg(to_json(data)), '[]') #>> '{}'
        FROM (
            SELECT
                *
            FROM
                server_ban_exemption
            WHERE
                user_id = @UserId
        ) as data
        """, cancel);
    }

    private async Task CollectServerRoleBan(Guid userId, ZipArchive into, CancellationToken cancel)
    {
        await CollectJson(userId, into, "server_role_ban", """
        SELECT
            COALESCE(json_agg(to_json(data)), '[]') #>> '{}'
        FROM (
            SELECT
                *,
                (SELECT to_jsonb(role_unban_sq) - 'ban_id' FROM (
                    SELECT * FROM server_role_unban WHERE server_role_unban.ban_id = server_role_ban.server_role_ban_id
                ) role_unban_sq)
                as unban
            FROM
                server_role_ban
            WHERE
                player_user_id = @UserId
        ) as data
        """, cancel);
    }

    private async Task CollectUploadedResourceLog(Guid userId, ZipArchive into, CancellationToken cancel)
    {
        await CollectJson(userId, into, "uploaded_resource_log", """
        SELECT
            COALESCE(json_agg(to_json(data)), '[]') #>> '{}'
        FROM (
            SELECT
                *
            FROM
                uploaded_resource_log
            WHERE
                user_id = @UserId
        ) as data
        """, cancel);
    }

    private async Task CollectWhitelist(Guid userId, ZipArchive into, CancellationToken cancel)
    {
        await CollectJson(userId, into, "whitelist", """
        SELECT
            COALESCE(json_agg(to_json(data)), '[]') #>> '{}'
        FROM (
            SELECT
                *
            FROM
                whitelist
            WHERE
                user_id = @UserId
        ) as data
        """, cancel);
    }

    private async Task CollectAdminMessages(Guid userId, ZipArchive into, CancellationToken cancel)
    {
        await CollectJson(userId, into, "admin_messages", """
        SELECT
            COALESCE(json_agg(to_json(data)), '[]') #>> '{}'
        FROM (
            SELECT
                *
            FROM
                whitelist
            WHERE
                user_id = @UserId
        ) as data
        """, cancel);
    }

    private async Task CollectAdminWatchlists(Guid userId, ZipArchive into, CancellationToken cancel)
    {
        await CollectJson(userId, into, "admin_watchlists", """
        SELECT
            COALESCE(json_agg(to_json(data)), '[]') #>> '{}'
        FROM (
            SELECT
                *
            FROM
                admin_watchlists
            WHERE
                player_user_id = @UserId
        ) as data
        """, cancel);
    }

    private async Task CollectRoleWhitelist(Guid userId, ZipArchive into, CancellationToken cancel)
    {
        await CollectJson(userId, into, "role_whitelist", """
        SELECT
            COALESCE(json_agg(to_json(data)), '[]') #>> '{}'
        FROM (
            SELECT
                *
            FROM
                role_whitelists
            WHERE
                player_user_id = @UserId
        ) as data
        """, cancel);
    }

    private async Task CollectJson(Guid userId, ZipArchive into, string name, string query, CancellationToken cancel)
    {
        var con = dbContext.Database.GetDbConnection();
        var result = await con.QuerySingleAsync<string>(new CommandDefinition(query, new
        {
            UserId = userId
        }, cancellationToken: cancel));

        var entry = into.CreateEntry($"{name}.json");
        using var writer = new StreamWriter(entry.Open());
        writer.Write(result);
    }
}
