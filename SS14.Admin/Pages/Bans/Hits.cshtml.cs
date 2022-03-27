using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Helpers;
using SS14.Admin.Pages.Connections;

namespace SS14.Admin.Pages.Bans;

public class Hits : PageModel
{
    private readonly PostgresServerDbContext _dbContext;

    public BanHelper.BanJoin Ban { get; set; } = default!;

    public ISortState SortState { get; private set; } = default!;
    public PaginationState<ConnectionsIndexModel.Connection> Pagination { get; } = new(100);
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


        Pagination.Init(pageIndex, perPage, AllRouteData);

        CurrentFilter = search;
        AllRouteData.Add("search", CurrentFilter);

        var logQuery = _dbContext.ServerBanHit
            .Include(b => b.Connection)
            .Where(bh => bh.BanId == banEntry.Ban.Id)
            .Select(bh => bh.Connection);

        logQuery = SearchHelper.SearchConnectionLog(logQuery, search);

        SortState = await ConnectionsIndexModel.LoadSortConnectionsTableData(Pagination, logQuery, sort, AllRouteData);

        return Page();
    }
}
