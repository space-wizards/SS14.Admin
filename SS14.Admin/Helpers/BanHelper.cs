using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;
using Content.Server.Database;
using Content.Shared.Database;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace SS14.Admin.Helpers;

/// <summary>
/// Shared logic between role and server bans.
/// </summary>
public sealed class BanHelper
{
    private readonly PostgresServerDbContext _dbContext;
    private readonly PlayerLocator _playerLocator;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BanHelper> _logger;
    private readonly IHttpContextAccessor _httpContext;

    public BanHelper(
        PostgresServerDbContext dbContext,
        PlayerLocator playerLocator,
        IConfiguration configuration,
        ILogger<BanHelper> logger,
        IHttpContextAccessor httpContext)
    {
        _dbContext = dbContext;
        _playerLocator = playerLocator;
        _configuration = configuration;
        _logger = logger;
        _httpContext = httpContext;
    }

    public IQueryable<BanJoin<ServerBan, ServerUnban>> CreateServerBanJoin()
    {
        return CreateBanJoin<ServerBan, ServerUnban>(_dbContext.Ban);
    }

    public IQueryable<BanJoin<ServerRoleBan, ServerRoleUnban>> CreateRoleBanJoin()
    {
        return CreateBanJoin<ServerRoleBan, ServerRoleUnban>(_dbContext.RoleBan);
    }

    private IQueryable<BanJoin<TBan, TUnban>> CreateBanJoin<TBan, TUnban>(DbSet<TBan> bans)
        where TBan : class, IBanCommon<TUnban>
        where TUnban : IUnbanCommon
    {
        return bans
            .Include(b => b.Unban)
            .LeftJoin(_dbContext.Player,
                ban => ban.PlayerUserId, player => player.UserId,
                (ban, player) => new { ban, player })
            .LeftJoin(_dbContext.Player,
                ban => ban.ban.BanningAdmin, admin => admin.UserId,
                (ban, admin) => new { ban.ban, ban.player, admin })
            .LeftJoin(_dbContext.Player,
                ban => ban.ban.Unban!.UnbanningAdmin, unbanAdmin => unbanAdmin.UserId,
                (ban, unbanAdmin) => new BanJoin<TBan, TUnban>
                {
                    Ban = ban.ban,
                    Player = ban.player,
                    Admin = ban.admin,
                    UnbanAdmin = unbanAdmin
                });
    }

    [Pure]
    public static bool IsBanActive<TUnban>(IBanCommon<TUnban> b) where TUnban : IUnbanCommon
    {
        return (b.ExpirationTime == null || b.ExpirationTime > DateTime.UtcNow) && b.Unban == null;
    }

    [Pure]
    [return: NotNullIfNotNull("hwid")]
    public static string? FormatHwid(ImmutableTypedHwid? hwid)
    {
        return hwid?.ToString();
    }

    public sealed class BanJoin<TBan, TUnban> where TBan: IBanCommon<TUnban> where TUnban : IUnbanCommon
    {
        public TBan Ban { get; set; } = default!;
        public Player? Player { get; set; }
        public Player? Admin { get; set; }
        public Player? UnbanAdmin { get; set; }
    }

    public async Task<(IPAddress address, ImmutableTypedHwid? hwid)?> GetLastPlayerInfo(string nameOrUid)
    {
        nameOrUid = nameOrUid.Trim();

        Player? player;
        if (Guid.TryParse(nameOrUid, out var guid))
        {
            player = await _dbContext.Player.SingleOrDefaultAsync(p => p.UserId == guid);
        }
        else
        {
            player = await _dbContext.Player
                .OrderByDescending(p => p.LastSeenTime)
                .FirstOrDefaultAsync(p => p.LastSeenUserName.ToUpper() == nameOrUid.ToUpper());
        }

        if (player == null)
            return null;

        return (player.LastSeenAddress, player.LastSeenHWId);
    }

    /// <returns>Non-null string if an error occured that must be reported to the user.</returns>
    public async Task<string?> FillBanCommon<TUnban>(
        IBanCommon<TUnban> ban,
        string? nameOrUid,
        string? ip,
        string? hwid,
        int lengthMinutes,
        string reason)
        where TUnban : IUnbanCommon
    {
        if (string.IsNullOrWhiteSpace(nameOrUid) && string.IsNullOrWhiteSpace(ip) && string.IsNullOrWhiteSpace(hwid))
            return "Must provide at least one of name/UID, IP address or HWID.";

        if (string.IsNullOrWhiteSpace(reason))
            return "Must provide reason.";

        if (!string.IsNullOrWhiteSpace(nameOrUid))
        {
            ban.PlayerUserId = await _playerLocator.Resolve(nameOrUid);
            if (ban.PlayerUserId == null)
                return $"Unable to find user with name {nameOrUid}";
        }

        if (!string.IsNullOrWhiteSpace(ip))
        {
            ip = ip.Trim();
            if (!IPHelper.TryParseIpOrCidr(ip, out var parsedAddr))
                return "Invalid IP address/CIDR range";

            var parsedIp = parsedAddr.Item1;
            var parsedCidr = parsedAddr.Item2;
            // Ban /64 on IPv6.
            parsedCidr ??= (byte)(parsedIp.AddressFamily == AddressFamily.InterNetwork ? 32 : 64);

            ban.Address = new NpgsqlInet(parsedIp, parsedCidr.Value);
        }

        if (!string.IsNullOrWhiteSpace(hwid))
        {
            hwid = hwid.Trim();
            if (!ImmutableTypedHwid.TryParse(hwid, out var parsedHwid))
                return "Invalid HWID";

            ban.HWId = parsedHwid;
        }

        if (lengthMinutes != 0)
        {
            ban.ExpirationTime = DateTime.UtcNow + TimeSpan.FromMinutes(lengthMinutes);
        }
        else
        {
            ban.ExpirationTime = null;
        }

        ban.BanningAdmin = _httpContext.HttpContext!.User.Claims.GetUserId();
        ban.Reason = reason;
        ban.BanTime = DateTime.UtcNow;
        return null;
    }
}
