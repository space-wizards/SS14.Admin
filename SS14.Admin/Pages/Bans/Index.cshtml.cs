using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Content.Server.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Helpers;
using SS14.Admin.Pages.Bans;

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

            var bans = BanHelper.CreateBanJoin(_dbContext)
                .Select(b => new {b.Ban, b.Player, b.Admin, b.UnbanAdmin, HitCount = b.Ban.BanHits.Count});

            if (!string.IsNullOrWhiteSpace(search))
            {
                if (Guid.TryParse(search, out var guid))
                {
                    bans = bans.Where(b => b.Ban.UserId == guid);
                }
                else if (IPHelper.TryParseCidr(search, out var cidr))
                {
                    bans = bans.Where(b => EF.Functions.ContainsOrEqual(cidr, b.Ban.Address!.Value));
                }
                else if (IPAddress.TryParse(search, out var ip))
                {
                    bans = bans.Where(u => EF.Functions.ContainsOrEqual(u.Ban.Address!.Value, ip));
                }
                else
                {
                    var normalized = search.ToUpperInvariant();
                    bans = bans.Where(u =>
                        u.Player!.LastSeenUserName.ToUpper().Contains(normalized) ||
                        u.Admin!.LastSeenUserName.ToUpper().Contains(normalized));
                }
            }

            bans = show switch
            {
                ShowFilter.Active => bans.Where(b => b.Ban.Unban == null && (b.Ban.ExpirationTime == null || b.Ban.ExpirationTime > DateTime.Now)),
                ShowFilter.Expired => bans.Where(b => b.Ban.Unban != null || b.Ban.ExpirationTime < DateTime.Now),
                _ => bans
            };

            CurrentFilter = search;
            Show = show;
            AllRouteData.Add("search", CurrentFilter);
            AllRouteData.Add("show", Show.ToString());

            var sortState = Helpers.SortState.Build(bans);
            sortState.AddColumn("name", p => p.Player!.LastSeenUserName);
            sortState.AddColumn("ip", p => p.Ban.Address);
            sortState.AddColumn("uid", p => p.Ban.UserId);
            sortState.AddColumn("time", p => p.Ban.BanTime, SortOrder.Descending);
            // sortState.AddColumn("expire_time", p => p.ban.Unban == null ? p.ban.ExpirationTime : p.ban.Unban!.UnbanTime);
            sortState.AddColumn("admin", p => p.Admin!.LastSeenUserName);
            sortState.Init(sort, AllRouteData);

            SortState = sortState;

            bans = sortState.ApplyToQuery(bans);

            await Pagination.LoadLinqAsync(bans, e => e.Select(b =>
            {
                (DateTime Time, string Admin)? unbanned = null;
                if (b.Ban.Unban != null)
                {
                    var time = b.Ban.Unban.UnbanTime;
                    var admin = b.UnbanAdmin?.LastSeenUserName ?? b.Ban.Unban.UnbanningAdmin.ToString()!;

                    unbanned = (time, admin);
                }

                return new Ban(
                    b.Ban.Id,
                    b.Player?.LastSeenUserName,
                    b.Ban.UserId?.ToString(),
                    b.Ban.Address?.FormatCidr(),
                    b.Ban.HWId is { } h ? Convert.ToBase64String(h) : null,
                    b.Ban.Reason,
                    b.Ban.ExpirationTime,
                    unbanned,
                    BanHelper.IsBanActive(b.Ban),
                    b.Ban.BanTime,
                    b.Admin?.LastSeenUserName,
                    b.HitCount);
            }));
        }

        public async Task<IActionResult> OnPostUnbanAsync([FromForm] UnbanModel model)
        {
            if (!User.IsInRole("BAN"))
                return Forbid();

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

            ban.Unban = new ServerUnban
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
            string? Hwid,
            string Reason,
            DateTime? Expires,
            (DateTime Time, string Admin)? Unbanned,
            bool Active,
            DateTime BanTime,
            string? Admin,
            int hitCount);

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
