using Content.Server.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SS14.Admin.Helpers;
using WhitelistJoin = SS14.Admin.Helpers.WhitelistHelper.WhitelistJoin;

namespace SS14.Admin.Pages.Whitelist;

public sealed class Index : PageModel
{
    private readonly PostgresServerDbContext _dbContext;

    public ISortState SortState { get; private set; } = default!;
    public PaginationState<WhitelistJoin> Pagination { get; } = new(100);
    public Dictionary<string, string?> AllRouteData { get; } = new();

    public string? CurrentFilter { get; set; }

    [TempData] public string? StatusMessage { get; set; }

    public Index(PostgresServerDbContext dbContext)
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

        var whitelist =  WhitelistHelper.MakeWhitelistJoin(_dbContext);
        whitelist = SearchHelper.SearchWhitelist(whitelist, search);

        CurrentFilter = search;

        AllRouteData.Add("search", CurrentFilter);

        var sortState = Helpers.SortState.Build(whitelist);
        sortState.AddColumn("name", p => p.Player!.LastSeenUserName, SortOrder.Ascending);
        sortState.AddColumn("uid", p => p.Whitelist.UserId);
        sortState.Init(sort, AllRouteData);

        SortState = sortState;

        whitelist = sortState.ApplyToQuery(whitelist);

        await Pagination.LoadAsync(whitelist);
    }
}
