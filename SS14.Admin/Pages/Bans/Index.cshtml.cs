using Content.Server.Database;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Helpers;

namespace SS14.Admin.Pages
{
    [ValidateAntiForgeryToken]
    public class BansModel : PageModel
    {
        private readonly PostgresServerDbContext _dbContext;
        private readonly BanHelper _banHelper;

        public ISortState SortState { get; private set; } = default!;
        public PaginationState<Ban> Pagination { get; } = new(100);
        public Dictionary<string, string?> AllRouteData { get; } = new();

        public string? CurrentFilter { get; set; }
        public ShowFilter? Show { get; set; }


        public BansModel(PostgresServerDbContext dbContext, BanHelper banHelper)
        {
            _dbContext = dbContext;
            _banHelper = banHelper;
        }

        public async Task OnGetAsync(
            string? sort,
            string? search,
            int? pageIndex,
            int? perPage,
            ShowFilter show = ShowFilter.Active)
        {
            Pagination.Init(pageIndex, perPage, AllRouteData);

            var bans = SearchHelper.SearchServerBans(_banHelper.CreateServerBanJoin(), search, User);

            bans = show switch
            {
                ShowFilter.Active => bans.Where(b =>
                    b.Ban.Unban == null && (b.Ban.ExpirationTime == null || b.Ban.ExpirationTime > DateTime.UtcNow)),
                ShowFilter.Expired => bans.Where(b => b.Ban.Unban != null || b.Ban.ExpirationTime < DateTime.UtcNow),
                _ => bans
            };

            CurrentFilter = search;
            Show = show;
            AllRouteData.Add("search", CurrentFilter);
            AllRouteData.Add("show", Show.ToString());

            SortState = await LoadSortBanTableData(Pagination, bans, sort, AllRouteData);
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

        [MustUseReturnValue]
        public static async Task<ISortState> LoadSortBanTableData(
            PaginationState<Ban> pagination,
            IQueryable<BanHelper.BanJoin<ServerBan, ServerUnban>> query,
            string? sort,
            Dictionary<string, string?> allRouteData)
        {
            var bans = query
                .Select(b => new { b.Ban, b.Player, b.Admin, b.UnbanAdmin, HitCount = b.Ban.BanHits.Count });

            var sortState = Helpers.SortState.Build(bans);
            sortState.AddColumn("name", p => p.Player!.LastSeenUserName);
            sortState.AddColumn("ip", p => p.Ban.Address);
            sortState.AddColumn("uid", p => p.Ban.PlayerUserId);
            sortState.AddColumn("time", p => p.Ban.BanTime, SortOrder.Descending);
            sortState.AddColumn("round", p => p.Ban.RoundId);
            // sortState.AddColumn("expire_time", p => p.ban.Unban == null ? p.ban.ExpirationTime : p.ban.Unban!.UnbanTime);
            sortState.AddColumn("admin", p => p.Admin!.LastSeenUserName);
            sortState.AddColumn("hits", p => p.HitCount);
            sortState.Init(sort, allRouteData);

            bans = sortState.ApplyToQuery(bans);

            await pagination.LoadLinqAsync(bans, e => e.Select(b =>
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
                    b.Player,
                    b.Ban.PlayerUserId?.ToString(),
                    b.Ban.Address?.FormatCidr(),
                    b.Ban.HWId?.ToImmutable().ToString(),
                    b.Ban.Reason,
                    b.Ban.ExpirationTime,
                    unbanned,
                    BanHelper.IsBanActive(b.Ban),
                    b.Ban.BanTime,
                    b.Admin?.LastSeenUserName,
                    b.HitCount,
                    b.Ban.RoundId);
            }));

            return sortState;
        }

        public sealed record Ban(
            int Id,
            Player? Player,
            string? UserId,
            string? Address,
            string? Hwid,
            string Reason,
            DateTime? Expires,
            (DateTime Time, string Admin)? Unbanned,
            bool Active,
            DateTime BanTime,
            string? Admin,
            int hitCount,
            int? Round);

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
