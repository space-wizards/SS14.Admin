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
                lj => new { ban = lj.Left, player = lj.Right })
            .LeftJoin(dbContext.Player,
                ban => ban.ban.BanningAdmin, admin => admin.UserId,
                lj => new { lj.Left.ban, lj.Left.player, admin = lj.Right })
            .LeftJoin(dbContext.Player,
                ban => ban.ban.Unban!.UnbanningAdmin, unbanAdmin => unbanAdmin.UserId,
                lj => new BanJoin
                {
                    Ban = lj.Left.ban,
                    Player = lj.Left.player,
                    Admin = lj.Left.admin,
                    UnbanAdmin = lj.Right
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
        Expression<Func<LeftJoinObj<TLeft, TRight>, TOutput>> join)
    {
        return left
            .GroupJoin(right, leftKey, rightKey, (l, r) => new { l, r })
            .SelectMany(j => j.r.DefaultIfEmpty(), (j, r) => new LeftJoinObj<TLeft, TRight> { Left = j.l, Right = r })
            .Select(join);
    }

    public sealed class LeftJoinObj<TLeft, TRight>
    {
        public TLeft Left { get; set; } = default!;
        public TRight? Right { get; set; }
    }
}
