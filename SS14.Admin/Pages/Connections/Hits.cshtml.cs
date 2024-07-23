using System.Data;
using Content.Server.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Helpers;

namespace SS14.Admin.Pages.Connections;

public sealed class Hits : PageModel
{
    private readonly PostgresServerDbContext _dbContext;
    private readonly BanHelper _banHelper;

    public ConnectionLog Log { get; set; } = default!;
    public ISortState SortState { get; private set; } = default!;
    public PaginationState<BansModel.Ban> Pagination { get; } = new(100);
    public Dictionary<string, string?> AllRouteData { get; } = new();
    public string? CurrentFilter { get; set; }


    public Hits(PostgresServerDbContext dbContext, BanHelper banHelper)
    {
        _dbContext = dbContext;
        _banHelper = banHelper;
    }

    public async Task<IActionResult> OnGetAsync(
        int connection,
        string? sort,
        string? search,
        int? pageIndex,
        int? perPage)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);

        var logEntry = await _dbContext.ConnectionLog.SingleOrDefaultAsync(c => c.Id == connection);

        if (logEntry == null)
            return NotFound();

        Log = logEntry;

        CurrentFilter = search;
        AllRouteData.Add("search", CurrentFilter);
        AllRouteData.Add("connection", connection.ToString());

        Pagination.Init(pageIndex, perPage, AllRouteData);

        var banQuery = SearchHelper.SearchServerBans(_banHelper.CreateServerBanJoin(), search, User)
            .Join(_dbContext.ServerBanHit, bj => bj.Ban.Id, bh => bh.BanId, (join, hit) => new
            {
                join, hit
            })
            .Where(bh => bh.hit.ConnectionId == logEntry.Id)
            .Select(bh => bh.join);

        SortState = await BansModel.LoadSortBanTableData(Pagination, banQuery, sort, AllRouteData);

        return Page();
    }
}
