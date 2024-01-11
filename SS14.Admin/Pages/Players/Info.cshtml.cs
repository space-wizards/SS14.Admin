using Content.Server.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Helpers;
using static SS14.Admin.Pages.BansModel;
using static SS14.Admin.Pages.RoleBans.Index;

namespace SS14.Admin.Pages.Players;

public sealed class Info : PageModel
{
    private readonly PostgresServerDbContext _dbContext;
    private readonly BanHelper _banHelper;

    public bool Whitelisted { get; set; }
    public Player Player { get; set; } = default!;
    public AdminNote[] Notes { get; set; } = default!;
    public PlayTime[] PlayTimes { get; set; } = default!;
    public Profile[] Profiles { get; set; } = default!;
    public ISortState RoleSortState { get; private set; } = default!;
    public ISortState GameSortState { get; private set; } = default!;
    public PaginationState<Ban> GameBanPagination { get; } = new(100);
    public PaginationState<RoleBan> RoleBanPagination { get; } = new(100);
    public Dictionary<string, string?> GameBanRouteData { get; } = new();
    public Dictionary<string, string?> RoleBanRouteData { get; } = new();
    public Info(PostgresServerDbContext dbContext, BanHelper banHelper)
    {
        _dbContext = dbContext;
        _banHelper = banHelper;
    }

    public async Task<IActionResult> OnGetAsync(
        Guid userId,
        int? pageIndex,
        int? perPage,
        string? sort)
    {
        GameBanPagination.Init(pageIndex, perPage, GameBanRouteData);
        RoleBanPagination.Init(pageIndex, perPage, RoleBanRouteData);

        var gameBans = SearchHelper.SearchServerBans(_banHelper.CreateServerBanJoin(), userId.ToString());
        var roleBans = SearchHelper.SearchRoleBans(_banHelper.CreateRoleBanJoin(), userId.ToString());

        GameBanRouteData.Add("search", userId.ToString());
        GameBanRouteData.Add("show", "all");
        RoleBanRouteData.Add("search", userId.ToString());
        RoleBanRouteData.Add("show", "all");

        if (sort == "role")
        {
            GameSortState = await LoadSortBanTableData(GameBanPagination, gameBans, default, GameBanRouteData);
            RoleSortState = await LoadSortBanTableData(RoleBanPagination, roleBans, sort, RoleBanRouteData);
        }
        else
        {
            GameSortState = await LoadSortBanTableData(GameBanPagination, gameBans, sort, GameBanRouteData);
            RoleSortState = await LoadSortBanTableData(RoleBanPagination, roleBans, sort, RoleBanRouteData);
        }

        var player = await _dbContext.Player.SingleOrDefaultAsync(a => a.UserId == userId);
        if (player == null)
            return NotFound();

        Player = player;

        Notes = await _dbContext.AdminNotes
            .Where(n => n.PlayerUserId == userId)
            .Include(n => n.CreatedBy)
            .Include(n => n.LastEditedBy)
            .OrderByDescending(n => n.CreatedAt)
            .ToArrayAsync();

        PlayTimes = await _dbContext.PlayTime
            .Where(t => t.PlayerId == userId)
            .ToArrayAsync();

        Profiles = await _dbContext.Profile
            .Where(t => t.Preference.UserId == userId)
            .OrderBy(t => t.Slot)
            .Include(t => t.Jobs)
            .ToArrayAsync();

        Whitelisted = await _dbContext.Whitelist.AnyAsync(p => p.UserId == userId);

        return Page();
    }
}
