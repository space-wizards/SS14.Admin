using System;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Pages.Bans;

namespace SS14.Admin.Helpers;

public static class SearchHelper
{
    public static IQueryable<ConnectionLog> SearchConnectionLog(IQueryable<ConnectionLog> query, string? search)
    {
        if (string.IsNullOrEmpty(search))
            return query;

        search = search.Trim();

        var normalized = search.ToUpperInvariant();
        Expression<Func<ConnectionLog, bool>> expr = u => u.UserName.ToUpper().Contains(normalized);

        if (Guid.TryParse(search, out var guid))
            CombineSearch(ref expr, u => u.UserId == guid);

        if (IPHelper.TryParseCidr(search, out var cidr))
            CombineSearch(ref expr, u => EF.Functions.Contains(cidr, u.Address));

        if (IPAddress.TryParse(search, out var ip))
            CombineSearch(ref expr, u => u.Address.Equals(ip));

        var hwid = new byte[Constants.HwidLength];
        if (Convert.TryFromBase64String(search, hwid, out var len) && len == Constants.HwidLength)
            CombineSearch(ref expr, u => u.HWId == hwid);

        return query.Where(expr);
    }

    public static IQueryable<BanHelper.BanJoin> SearchServerBans(
        IQueryable<BanHelper.BanJoin> query,
        string? search)
    {
        if (string.IsNullOrEmpty(search))
            return query;

        search = search.Trim();
        var normalized = search.ToUpperInvariant();
        Expression<Func<BanHelper.BanJoin, bool>> expr = u =>
            u.Player!.LastSeenUserName.ToUpper().Contains(normalized) ||
            u.Admin!.LastSeenUserName.ToUpper().Contains(normalized);

        if (Guid.TryParse(search, out var guid))
            CombineSearch(ref expr, b => b.Ban.UserId == guid);

        if (IPHelper.TryParseCidr(search, out var cidr))
            CombineSearch(ref expr, b => EF.Functions.ContainsOrEqual(cidr, b.Ban.Address!.Value));

        if (IPAddress.TryParse(search, out var ip))
            CombineSearch(ref expr, u => EF.Functions.ContainsOrEqual(u.Ban.Address!.Value, ip));

        var hwid = new byte[Constants.HwidLength];
        if (Convert.TryFromBase64String(search, hwid, out var len) && len == Constants.HwidLength)
            CombineSearch(ref expr, u => u.Ban.HWId == hwid);

        return query.Where(expr);
    }

    public static IQueryable<Player> SearchPlayers(IQueryable<Player> query, string? search)
    {
        if (string.IsNullOrEmpty(search))
            return query;

        search = search.Trim();

        var normalized = search.ToUpperInvariant();
        Expression<Func<Player, bool>> expr = u => u.LastSeenUserName.ToUpper().Contains(normalized);

        if (Guid.TryParse(search, out var guid))
            CombineSearch(ref expr, u => u.UserId == guid);

        if (IPHelper.TryParseCidr(search, out var cidr))
            CombineSearch(ref expr, u => EF.Functions.Contains(cidr, u.LastSeenAddress));

        if (IPAddress.TryParse(search, out var ip))
            CombineSearch(ref expr, u => u.LastSeenAddress.Equals(ip));

        var hwid = new byte[Constants.HwidLength];
        if (Convert.TryFromBase64String(search, hwid, out var len) && len == Constants.HwidLength)
            CombineSearch(ref expr, u => u.LastSeenHWId == hwid);

        return query.Where(expr);
    }

    public static void CombineSearch<T>(
        ref Expression<Func<T, bool>> a,
        Expression<Func<T, bool>> b)
    {
        var param = Expression.Parameter(typeof(T));
        var body = Expression.Or(Expression.Invoke(a, param), Expression.Invoke(b, param));
        a = Expression.Lambda<Func<T, bool>>(body, param);
    }
}
