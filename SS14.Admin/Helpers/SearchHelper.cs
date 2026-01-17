using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using Content.Server.Database;
using Content.Shared.Database;
using Microsoft.EntityFrameworkCore;
using WhitelistJoin = SS14.Admin.Helpers.WhitelistHelper.WhitelistJoin;

namespace SS14.Admin.Helpers;

public static class SearchHelper
{
    public static IQueryable<ConnectionLog> SearchConnectionLog(IQueryable<ConnectionLog> query, string? search, ClaimsPrincipal user)
    {
        if (string.IsNullOrEmpty(search))
            return query;

        search = search.Trim();

        var normalized = search.ToUpperInvariant();
        Expression<Func<ConnectionLog, bool>> expr = u => u.UserName.ToUpper().Contains(normalized);

        if (Guid.TryParse(search, out var guid))
            CombineSearch(ref expr, u => u.UserId == guid);

        if (user.IsInRole(Constants.PIIRole) && IPHelper.TryParseCidr(search, out var cidr))
            CombineSearch(ref expr, u => EF.Functions.Contains(cidr, u.Address));

        if (user.IsInRole(Constants.PIIRole) && IPAddress.TryParse(search, out var ip))
            CombineSearch(ref expr, u => u.Address.Equals(ip));

        if (user.IsInRole(Constants.PIIRole) && ImmutableTypedHwid.TryParse(search, out var hwid))
            CombineSearch(ref expr, u => u.HWId!.Type == hwid.Type && u.HWId.Hwid == hwid.Hwid.ToArray());

        return query.Where(expr);
    }

    private static Expression<Func<BanHelper.BanJoin, bool>> MakeCommonBanSearchExpression(
        string search, ClaimsPrincipal user)
    {
        var normalized = search.ToUpperInvariant();

        Expression<Func<BanHelper.BanJoin, bool>> expr = u =>
            u.Players.Any(p => p.LastSeenUserName.ToUpper().Contains(normalized)) ||
            u.Admin!.LastSeenUserName.ToUpper().Contains(normalized);

        if (Guid.TryParse(search, out var guid))
            CombineSearch(ref expr, b => b.Ban.Players!.Any(p => p.UserId == guid));

        if (user.IsInRole(Constants.PIIRole) && IPHelper.TryParseCidr(search, out var cidr))
            CombineSearch(ref expr, b => b.Ban.Addresses!.Any(a => EF.Functions.ContainsOrEqual(cidr, a.Address)));

        if (user.IsInRole(Constants.PIIRole) && IPAddress.TryParse(search, out var ip))
            CombineSearch(ref expr, u => u.Ban.Addresses!.Any(a => EF.Functions.ContainsOrEqual(a.Address, ip)));

        if (user.IsInRole(Constants.PIIRole) && ImmutableTypedHwid.TryParse(search, out var hwid))
            CombineSearch(ref expr, u => u.Ban.Hwids!.Any(h => h.HWId.Type == hwid.Type && h.HWId.Hwid == hwid.Hwid.ToArray()));

        return expr;
    }

    public static IQueryable<BanHelper.BanJoin> SearchServerBans(
        IQueryable<BanHelper.BanJoin> query,
        string? search, ClaimsPrincipal user)
    {
        if (string.IsNullOrEmpty(search))
            return query;

        search = search.Trim();

        var expr = MakeCommonBanSearchExpression(search, user);

        return query.Where(expr);
    }

    public static IQueryable<BanHelper.BanJoin> SearchRoleBans(
        IQueryable<BanHelper.BanJoin> query,
        string? search, ClaimsPrincipal user)
    {
        if (string.IsNullOrEmpty(search))
            return query;

        search = search.Trim();

        var expr = MakeCommonBanSearchExpression(search, user);

        // Match role name exactly.
        CombineSearch(ref expr, u => u.Ban.Roles!.Any(r => r.RoleId == search));

        return query.Where(expr);
    }

    public static IQueryable<Player> SearchPlayers(IQueryable<Player> query, string? search, ClaimsPrincipal user)
    {
        if (string.IsNullOrEmpty(search))
            return query;

        search = search.Trim();

        var normalized = search.ToUpperInvariant();
        Expression<Func<Player, bool>> expr = u => u.LastSeenUserName.ToUpper().Contains(normalized);

        if (Guid.TryParse(search, out var guid))
            CombineSearch(ref expr, u => u.UserId == guid);

        if (user.IsInRole(Constants.PIIRole) && IPHelper.TryParseCidr(search, out var cidr))
            CombineSearch(ref expr, u => EF.Functions.Contains(cidr, u.LastSeenAddress));

        if (user.IsInRole(Constants.PIIRole) && IPAddress.TryParse(search, out var ip))
            CombineSearch(ref expr, u => u.LastSeenAddress.Equals(ip));

        if (user.IsInRole(Constants.PIIRole) && ImmutableTypedHwid.TryParse(search, out var hwid))
            CombineSearch(ref expr, u => u.LastSeenHWId!.Type == hwid.Type && u.LastSeenHWId.Hwid == hwid.Hwid.ToArray());

        return query.Where(expr);
    }

    public static IQueryable<WhitelistJoin> SearchWhitelist(IQueryable<WhitelistJoin> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return query;

        search = search.Trim();

        var normalized = search.ToUpperInvariant();
        Expression<Func<WhitelistJoin, bool>> expr = u => u.Player!.LastSeenUserName.ToUpper().Contains(normalized);

        if (Guid.TryParse(search, out var guid))
            CombineSearch(ref expr, u => u.Whitelist.UserId == guid);

        return query.Where(expr);
    }

    private static void CombineSearch<T>(
        ref Expression<Func<T, bool>> a,
        Expression<Func<T, bool>> b)
    {
        var param = Expression.Parameter(typeof(T));
        var body = Expression.Or(Expression.Invoke(a, param), Expression.Invoke(b, param));
        a = Expression.Lambda<Func<T, bool>>(body, param);
    }
}
