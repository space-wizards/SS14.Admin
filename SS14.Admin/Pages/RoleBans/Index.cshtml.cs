using Content.Server.Database;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Helpers;

namespace SS14.Admin.Pages.RoleBans;

[ValidateAntiForgeryToken]
public class Index : PageModel
{
    private readonly PostgresServerDbContext _dbContext;
    private readonly BanHelper _banHelper;

    public ISortState SortState { get; private set; } = default!;
    public PaginationState<RoleBan> Pagination { get; } = new(100);
    public Dictionary<string, string?> AllRouteData { get; } = new();

    public string? CurrentFilter { get; set; }
    public ShowFilter? Show { get; set; }


    public Index(PostgresServerDbContext dbContext, BanHelper banHelper)
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

        var bans = SearchHelper.SearchRoleBans(_banHelper.CreateRoleBanJoin(), search, User);

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

        ban.Unban = new Unban
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
        PaginationState<RoleBan> pagination,
        IQueryable<BanHelper.BanJoin> query,
        string? sort,
        Dictionary<string, string?> allRouteData)
    {
        var bans = query.Select(b => new { b.Ban, b.Players, b.Admin, b.UnbanAdmin });

        var sortState = Helpers.SortState.Build(bans);
        sortState.AddColumnMultiple("name", b => b.Players.Select(p => p.LastSeenUserName));
        sortState.AddColumnMultiple("ip", b => b.Ban.Addresses!.Select(a => a.Address));
        sortState.AddColumnMultiple("uid", b => b.Ban.Players!.Select(p => p.UserId));
        sortState.AddColumn("time", p => p.Ban.BanTime, SortOrder.Descending);
        sortState.AddColumnMultiple("round", p => p.Ban.Rounds!.Select(r => r.RoundId));
        // sortState.AddColumn("expire_time", p => p.ban.Unban == null ? p.ban.ExpirationTime : p.ban.Unban!.UnbanTime);
        sortState.AddColumn("admin", p => p.Admin!.LastSeenUserName);
        sortState.AddColumnMultiple("role", p => p.Ban.Roles!.Select(br => string.Concat(br.RoleType, ":", br.RoleId)));
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

            return new RoleBan(
                b.Ban.Id,
                b.Players,
                b.Ban.Players!.Select(p => p.UserId.ToString()).ToArray(),
                b.Ban.Addresses!.Select(a => a.Address.FormatCidr().ToString()).ToArray(),
                b.Ban.Hwids!.Select(h => h.HWId.ToImmutable().ToString()).ToArray(),
                b.Ban.Reason,
                b.Ban.ExpirationTime,
                unbanned,
                BanHelper.IsBanActive(b.Ban),
                b.Ban.BanTime,
                b.Admin?.LastSeenUserName,
                b.Ban.Roles!.Select(br => string.Concat(br.RoleType, ":", br.RoleId)).ToArray(),
                b.Ban.Rounds!.Select(r => r.RoundId).ToArray());
        }));

        return sortState;
    }

    public sealed record RoleBan(
        int Id,
        Player[] Players,
        string[] UserIds,
        string[] Addresses,
        string[] Hwids,
        string Reason,
        DateTime? Expires,
        (DateTime Time, string Admin)? Unbanned,
        bool Active,
        DateTime BanTime,
        string? Admin,
        string[] Roles,
        int[] Rounds);

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
