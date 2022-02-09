using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;

namespace SS14.Admin.Pages.Bans;

public static class BanHelper
{
    public static IQueryable<BanJoin> CreateBanJoin(PostgresServerDbContext dbContext)
    {
        return dbContext.Ban
            .Include(b => b.Unban)
            .LeftJoin(dbContext.Player,
                ban => ban.UserId, player => player.UserId,
                (ban, player) => new { ban, player })
            .LeftJoin(dbContext.Player,
                ban => ban.ban.BanningAdmin, admin => admin.UserId,
                (ban, admin) => new { ban.ban, ban.player, admin })
            .LeftJoin(dbContext.Player,
                ban => ban.ban.Unban!.UnbanningAdmin, unbanAdmin => unbanAdmin.UserId,
                (ban, unbanAdmin) => new BanJoin
                {
                    Ban = ban.ban,
                    Player = ban.player,
                    Admin = ban.admin,
                    UnbanAdmin = unbanAdmin
                });
    }

    [Pure]
    public static bool IsBanActive(ServerBan b)
    {
        return (b.ExpirationTime == null || b.ExpirationTime > DateTime.Now) && b.Unban == null;
    }

    [Pure]
    [return: NotNullIfNotNull("hwid")]
    public static string? FormatHwid(byte[]? hwid)
    {
        return hwid is { } h ? Convert.ToBase64String(h) : null;
    }

    public sealed class BanJoin
    {
        public ServerBan Ban { get; set; } = default!;
        public Player? Player { get; set; }
        public Player? Admin { get; set; }
        public Player? UnbanAdmin { get; set; }
    }

    public static IQueryable<TOutput> LeftJoin<TLeft, TRight, TKey, TOutput>(
        this IQueryable<TLeft> left,
        IEnumerable<TRight> right,
        Expression<Func<TLeft, TKey>> leftKey,
        Expression<Func<TRight, TKey>> rightKey,
        Expression<Func<TLeft, TRight, TOutput>> join)
    {
        var paramJ = Expression.Parameter(typeof(LeftJoinInternal<TLeft, TRight>));
        var paramR = Expression.Parameter(typeof(TRight));
        var body = Expression.Invoke(join, Expression.Field(paramJ, "L"), paramR);
        var l = Expression.Lambda<Func<LeftJoinInternal<TLeft, TRight>, TRight, TOutput>>(body, paramJ, paramR);

        return left
            .GroupJoin(right, leftKey, rightKey, (l, r) => new LeftJoinInternal<TLeft, TRight> { L = l, R = r })
            .SelectMany(j => j.R.DefaultIfEmpty()!, l);
    }

    private sealed class LeftJoinInternal<TLeft, TRight>
    {
        public TLeft L = default!;
        public IEnumerable<TRight> R = default!;
    }
}
