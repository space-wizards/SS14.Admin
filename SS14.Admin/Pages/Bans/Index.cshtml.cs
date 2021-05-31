using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Content.Server.Database;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Helpers;

namespace SS14.Admin.Pages
{
    public class BansModel : PageModel
    {
        private readonly PostgresServerDbContext _dbContext;

        public ISortState SortState { get; private set; } = default!;
        public PaginationState<Ban> Pagination { get; } = new(100);
        public Dictionary<string, string?> AllRouteData { get; } = new();

        public string? CurrentFilter { get; set; }


        public BansModel(PostgresServerDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task OnGetAsync(
            string? sort,
            string? search,
            int? pageIndex,
            int? perPage)
        {
            Pagination.Init(pageIndex, perPage, AllRouteData);

            var bans = _dbContext.Ban
                .Include(b => b.Unban)
                .Join(
                    _dbContext.Player,
                    ban => ban.UserId, player => player.UserId,
                    (ban, player) => new {ban, player})
                .Join(
                    _dbContext.Player,
                    ban => ban.ban.BanningAdmin, admin => admin.UserId,
                    (ban, admin) => new {ban.ban, ban.player, admin});

            if (!string.IsNullOrWhiteSpace(search))
            {
                if (Guid.TryParse(search, out var guid))
                {
                    bans = bans.Where(b => b.ban.UserId == guid);
                }
                else if (IPHelper.TryParseCidr(search, out var cidr))
                {
                    bans = bans.Where(b => EF.Functions.ContainsOrEqual(cidr, b.ban.Address!.Value));
                }
                else if (IPAddress.TryParse(search, out var ip))
                {
                    bans = bans.Where(u => EF.Functions.ContainsOrEqual(u.ban.Address!.Value, ip));
                }
                else
                {
                    var normalized = search.ToUpperInvariant();
                    bans = bans.Where(u =>
                        u.player.LastSeenUserName.ToUpper().Contains(normalized) ||
                        u.admin.LastSeenUserName.ToUpper().Contains(normalized));
                }
            }

            CurrentFilter = search;
            AllRouteData.Add("search", CurrentFilter);

            var sortState = Helpers.SortState.Build(bans);
            sortState.AddColumn("name", p => p.player!.LastSeenUserName);
            sortState.AddColumn("ip", p => p.ban.Address);
            sortState.AddColumn("uid", p => p.ban.UserId);
            sortState.AddColumn("time", p => p.ban.BanTime, SortOrder.Descending);
            // sortState.AddColumn("expire_time", p => p.ban.Unban == null ? p.ban.ExpirationTime : p.ban.Unban!.UnbanTime);
            sortState.AddColumn("admin", p => p.admin.LastSeenUserName);
            sortState.Init(sort, AllRouteData);

            SortState = sortState;

            bans = sortState.ApplyToQuery(bans);

            await Pagination.LoadLinqAsync(bans, e => e.Select(b => new Ban(
                b.player?.LastSeenUserName,
                b.ban.UserId?.ToString(),
                b.ban.Address?.FormatCidr(),
                b.ban.Reason,
                b.ban.BanTime,
                b.admin?.LastSeenUserName)));
        }

        public sealed record Ban(
            string? Name,
            string? UserId,
            string? Address,
            string Reason,
            DateTime BanTime,
            string? Admin);
    }
}