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

    public IQueryable<BanJoin> CreateServerBanJoin()
    {
        return CreateBanJoin().Where(b => b.Ban.Type == BanType.Server);
    }

    public IQueryable<BanJoin> CreateRoleBanJoin()
    {
        return CreateBanJoin().Where(b => b.Ban.Type == BanType.Role);
    }

    private IQueryable<BanJoin> CreateBanJoin()
    {
        return _dbContext.Ban
            .AsSplitQuery()
            .Include(b => b.Unban)
            .Include(b => b.Players)
            .Include(b => b.Addresses)
            .Include(b => b.Hwids)
            .Include(b => b.Roles)
            .Include(b => b.Rounds)
            .LeftJoin(_dbContext.Player,
                ban => ban.BanningAdmin, admin => admin.UserId,
                (ban, admin) => new { ban, admin })
            .LeftJoin(_dbContext.Player,
                ban => ban.ban.Unban!.UnbanningAdmin, unbanAdmin => unbanAdmin.UserId,
                (ban, unbanAdmin) => new BanJoin
                {
                    Players = ban.ban.Players!
                        .Select(p => _dbContext.Player.SingleOrDefault(pp => p.UserId == pp.UserId))
                        .Where(p => p != null)
                        .ToArray()!,
                    Ban = ban.ban,
                    Admin = ban.admin,
                    UnbanAdmin = unbanAdmin
                });
    }

    [Pure]
    public static bool IsBanActive(Ban b)
    {
        return (b.ExpirationTime == null || b.ExpirationTime > DateTime.UtcNow) && b.Unban == null;
    }

    [Pure]
    [return: NotNullIfNotNull("hwid")]
    public static string? FormatHwid(ImmutableTypedHwid? hwid)
    {
        return hwid?.ToString();
    }

    public sealed class BanJoin
    {
        public Ban Ban { get; set; } = default!;
        public Player? Admin { get; set; }
        public Player[] Players { get; set; } = [];
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
    public async Task<string?> FillBanCommon(
        Ban ban,
        string? nameOrUid,
        string? ip,
        string? hwid,
        int lengthMinutes,
        string reason)
    {
        if (string.IsNullOrWhiteSpace(nameOrUid) && string.IsNullOrWhiteSpace(ip) && string.IsNullOrWhiteSpace(hwid))
            return "Must provide at least one of name/UID, IP address or HWID.";

        if (string.IsNullOrWhiteSpace(reason))
            return "Must provide reason.";

        if (!string.IsNullOrWhiteSpace(nameOrUid))
        {
            var userId = await _playerLocator.Resolve(nameOrUid);
            if (userId == null)
                return $"Unable to find user with name {nameOrUid}";

            ban.Players =
            [
                new BanPlayer
                {
                    UserId = userId.Value,
                }
            ];
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

            ban.Addresses =
            [
                new BanAddress
                {
                    Address = new NpgsqlInet(parsedIp, parsedCidr.Value)
                }
            ];
        }

        if (!string.IsNullOrWhiteSpace(hwid))
        {
            hwid = hwid.Trim();
            if (!ImmutableTypedHwid.TryParse(hwid, out var parsedHwid))
                return "Invalid HWID";

            ban.Hwids =
            [
                new BanHwid
                {
                    HWId = parsedHwid,
                }
            ];
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
