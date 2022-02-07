using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Helpers;

namespace SS14.Admin.Pages.Bans;

public class Hits : PageModel
{
    private readonly PostgresServerDbContext _dbContext;

    public BanHelper.BanJoin Ban { get; set; } = default!;

    public SortState<ConnectionLog> SortState { get; } = new();
    public PaginationState<ConnectionLog> Pagination { get; } = new(100);
    public Dictionary<string, string?> AllRouteData { get; } = new();

    public string? CurrentFilter { get; set; }

    public Hits(PostgresServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> OnGetAsync(
        int ban,
        string? sort,
        string? search,
        int? pageIndex,
        int? perPage)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);

        var banEntry = await BanHelper.CreateBanJoin(_dbContext).SingleOrDefaultAsync(b => b.Ban.Id == ban);

        if (banEntry == null)
            return NotFound();

        Ban = banEntry;

        SortState.AddColumn("name", c => c.UserName);
        SortState.AddColumn("uid", c => c.UserId);
        SortState.AddColumn("time", c => c.Time, SortOrder.Descending);
        SortState.AddColumn("addr", c => c.Address);
        SortState.AddColumn("hwid", c => c.HWId);
        SortState.AddColumn("denied", c => c.Denied);
        SortState.Init(sort, AllRouteData);

        Pagination.Init(pageIndex, perPage, AllRouteData);

        CurrentFilter = search;
        AllRouteData.Add("search", CurrentFilter);

        var logQuery = _dbContext.ServerBanHit
            .Include(b => b.Connection)
            .Where(bh => bh.BanId == banEntry.Ban.Id)
            .Select(bh => bh.Connection);

        logQuery = SearchHelper.SearchConnectionLog(logQuery, search);

        var sortedQuery = SortState.ApplyToQuery(logQuery).ThenByDescending(s => s.Time);

        await Pagination.LoadAsync(sortedQuery);

        return Page();
    }
}
