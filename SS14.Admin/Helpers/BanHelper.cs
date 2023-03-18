using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Net;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;

namespace SS14.Admin.Helpers;

/// <summary>
/// Shared logic between role and server bans.
/// </summary>
public sealed class BanHelper
{
    private readonly PostgresServerDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BanHelper> _logger;
    private readonly IHttpContextAccessor _httpContext;

    public BanHelper(
        PostgresServerDbContext dbContext,
        IConfiguration configuration,
        ILogger<BanHelper> logger,
        IHttpContextAccessor httpContext)
    {
        _dbContext = dbContext;
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
                ban => ban.UserId, player => player.UserId,
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
        return (b.ExpirationTime == null || b.ExpirationTime > DateTime.Now) && b.Unban == null;
    }

    [Pure]
    [return: NotNullIfNotNull("hwid")]
    public static string? FormatHwid(byte[]? hwid)
    {
        return hwid is { } h ? Convert.ToBase64String(h) : null;
    }

    public sealed class BanJoin<TBan, TUnban> where TBan: IBanCommon<TUnban> where TUnban : IUnbanCommon
    {
        public TBan Ban { get; set; } = default!;
        public Player? Player { get; set; }
        public Player? Admin { get; set; }
        public Player? UnbanAdmin { get; set; }
    }

    public async Task<(IPAddress address, byte[]? hwid)?> GetLastPlayerInfo(string nameOrUid)
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
            return "Error: Must provide at least one of name/UID, IP address or HWID.";

        if (string.IsNullOrWhiteSpace(reason))
            return "Error: Must provide reason.";

        if (!string.IsNullOrWhiteSpace(nameOrUid))
        {
            nameOrUid = nameOrUid.Trim();
            if (Guid.TryParse(nameOrUid, out var guid))
            {
                ban.UserId = guid;
            }
            else
            {
                try
                {
                    ban.UserId = await FindPlayerGuidByNameAsync(nameOrUid);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Unable to fetch user ID from auth server");
                    return "Error: Unknown error occured fetching user ID from auth server.";
                }

                if (ban.UserId == null)
                {
                    return $"Error: Unable to find user with name {nameOrUid}";
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(ip))
        {
            ip = ip.Trim();
            if (!IPHelper.TryParseIpOrCidr(ip, out var parsedAddr))
                return "Error: Invalid IP address/CIDR range";

            ban.Address = parsedAddr;
        }

        if (!string.IsNullOrWhiteSpace(hwid))
        {
            hwid = hwid.Trim();
            ban.HWId = new byte[Constants.HwidLength];

            if (!Convert.TryFromBase64String(hwid, ban.HWId, out _))
                return "Error: Invalid HWID";
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

    private async Task<Guid?> FindPlayerGuidByNameAsync(string name)
    {
        // Try our own database first, in case this is a guest or something.

        var player = await _dbContext.Player
            .Where(p => p.LastSeenUserName == name)
            .OrderByDescending(p => p.LastSeenTime)
            .FirstOrDefaultAsync();

        if (player != null)
            return player.UserId;

        var server = _configuration["AuthServer"];
        var client = new HttpClient();

        var url = $"{server}/api/query/name?name={Uri.EscapeDataString(name)}";

        var resp = await client.GetAsync(url);
        if (resp.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        resp.EnsureSuccessStatusCode();

        return (await resp.Content.ReadFromJsonAsync<QueryUserResponse>())!.UserId;
    }

    private sealed record QueryUserResponse(
        string UserName,
        Guid UserId,
        string? PatronTier,
        DateTimeOffset CreatedTime);
}
