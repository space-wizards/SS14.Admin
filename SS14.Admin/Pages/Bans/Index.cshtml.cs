using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Content.Server.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
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
        public ShowFilter? Show { get; set; }


        public BansModel(PostgresServerDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task OnGetAsync(
            string? sort,
            string? search,
            int? pageIndex,
            int? perPage,
            ShowFilter show=ShowFilter.Active)
        {
            Pagination.Init(pageIndex, perPage, AllRouteData);

            var bans = _dbContext.Ban
                .Include(b => b.Unban)
                // Confusing-ass EFCore left joins
                .GroupJoin(_dbContext.Player,
                    ban => ban.UserId, player => player.UserId,
                    (ban, player) => new {ban, player})
                .SelectMany(b => b.player.DefaultIfEmpty(), (g, player) => new {g.ban, player})
                .GroupJoin(
                    _dbContext.Player,
                    ban => ban.ban.BanningAdmin, admin => admin.UserId,
                    (ban, admin) => new {ban.ban, ban.player, admin})
                .SelectMany(b => b.admin.DefaultIfEmpty(), (g, admin) => new {g.ban, g.player, admin})
                .GroupJoin(
                    _dbContext.Player,
                    ban => ban.ban.Unban!.UnbanningAdmin, unbanAdmin => unbanAdmin.UserId,
                    (ban, unbanAdmin) => new {ban.ban, ban.player, ban.admin, unbanAdmin})
                .SelectMany(b => b.unbanAdmin.DefaultIfEmpty(), (g, unbanAdmin) => new {g.ban, g.player, g.admin, unbanAdmin});

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

            bans = show switch
            {
                ShowFilter.Active => bans.Where(b => b.ban.Unban == null && (b.ban.ExpirationTime == null || b.ban.ExpirationTime > DateTime.Now)),
                ShowFilter.Expired => bans.Where(b => b.ban.Unban != null || b.ban.ExpirationTime < DateTime.Now),
                _ => bans
            };

            CurrentFilter = search;
            Show = show;
            AllRouteData.Add("search", CurrentFilter);
            AllRouteData.Add("show", Show.ToString());

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

            await Pagination.LoadLinqAsync(bans, e => e.Select(b =>
            {
                var expires = "PERMANENT";
                if (b.ban.ExpirationTime != null)
                    expires = b.ban.ExpirationTime.ToString()!;

                (DateTime Time, string Admin)? unbanned = null;
                if (b.ban.Unban != null)
                {
                    var time = b.ban.Unban.UnbanTime;
                    var admin = b.unbanAdmin?.LastSeenUserName ?? b.ban.Unban.UnbanningAdmin.ToString()!;

                    unbanned = (time, admin);
                }

                var active = b.ban.ExpirationTime < DateTime.Now || b.ban.Unban != null;

                return new Ban(
                    b.ban.Id,
                    b.player?.LastSeenUserName,
                    b.ban.UserId?.ToString(),
                    b.ban.Address?.FormatCidr(),
                    b.ban.Reason,
                    expires,
                    unbanned,
                    active,
                    b.ban.BanTime,
                    b.admin?.LastSeenUserName);
            }));
        }

        public async Task<IActionResult> OnPostUnbanAsync([FromForm] UnbanModel model)
        {
            var id = model.Id;

            var ban = await _dbContext.Ban
                .Include(b => b.Unban)
                .SingleOrDefaultAsync(b => b.Id == id);

            if (ban == null)
            {
                TempData.Add("StatusMessage", "Error: Unable to find ban");
                return RedirectToPage("./Index");
            }

            if (ban.Unban != null)
            {
                TempData.Add("StatusMessage", "Error: Already unbanned");
                return RedirectToPage("./Index");
            }

            ban.Unban = new PostgresServerUnban
            {
                Ban = ban,
                UnbanningAdmin = User.Claims.GetUserId(),
                UnbanTime = DateTime.UtcNow
            };

            await _dbContext.SaveChangesAsync();
            TempData.Add("StatusMessage", "Unban done");
            return RedirectToPage("./Index");
        }

        public sealed record Ban(
            int Id,
            string? Name,
            string? UserId,
            string? Address,
            string Reason,
            string Expires,
            (DateTime Time, string Admin)? Unbanned,
            bool Active,
            DateTime BanTime,
            string? Admin);

        public enum ShowFilter
        {
            Active = 0,
            Expired = 1,
            All = 2,
        }

        public sealed class UnbanModel
        {
            public int Id { get; set; }
        }
    }
}
