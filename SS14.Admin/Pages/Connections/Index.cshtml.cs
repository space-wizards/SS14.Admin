using Content.Server.Database;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SS14.Admin.Helpers;

namespace SS14.Admin.Pages.Connections
{
    public class ConnectionsIndexModel : PageModel
    {
        private readonly PostgresServerDbContext _dbContext;

        public ISortState SortState { get; private set; } = default!;
        public PaginationState<Connection> Pagination { get; } = new(100);
        public Dictionary<string, string?> AllRouteData { get; } = new();

        public string? CurrentFilter { get; set; }
        public bool ShowAccepted { get; set; }
        public bool ShowBanned { get; set; }
        public bool ShowWhitelist { get; set; }
        public bool ShowFull { get; set; }
        public bool ShowPanic { get; set; }

        public ConnectionsIndexModel(PostgresServerDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task OnGetAsync(
            string? sort,
            string? search,
            int? pageIndex,
            int? perPage,
            bool showSet,
            bool showAccepted,
            bool showBanned,
            bool showWhitelist,
            bool showFull,
            bool showPanic)
        {

            Pagination.Init(pageIndex, perPage, AllRouteData);

            if (!showSet)
            {
                // So whoever designed <input> checkboxes was clearly dunked in the head with a frying pan.
                // A checkbox that is unchecked is NEVER sent in the form/query params.
                // This means it is impossible for us to distinguish between "this box is explicitly unchecked" and
                // "not set use default value".
                // Thank you HTML, you never fail to be fucking stupid.
                // Use the showSet query param to indicate a "these checkboxes have explicit values".
                // Ugh.
                showAccepted = true;
                showBanned = true;
                showWhitelist = true;
                showFull = true;
                showPanic = true;
            }

            CurrentFilter = search;
            ShowAccepted = showAccepted;
            ShowBanned = showBanned;
            ShowWhitelist = showWhitelist;
            ShowFull = showFull;
            ShowPanic = showPanic;

            AllRouteData.Add("search", CurrentFilter);
            AllRouteData.Add("showAccepted", showAccepted.ToString());
            AllRouteData.Add("showBanned", showBanned.ToString());
            AllRouteData.Add("showWhitelist", showWhitelist.ToString());
            AllRouteData.Add("showFull", showFull.ToString());
            AllRouteData.Add("showPanic", showPanic.ToString());
            AllRouteData.Add("showSet", "true");

            IQueryable<ConnectionLog> logQuery = _dbContext.ConnectionLog;
            logQuery = SearchHelper.SearchConnectionLog(logQuery, search, User);

            var acceptableDenies = new List<ConnectionDenyReason?>();
            if (showAccepted)
                acceptableDenies.Add(null);
            if (showBanned)
                acceptableDenies.Add(ConnectionDenyReason.Ban);
            if (showWhitelist)
                acceptableDenies.Add(ConnectionDenyReason.Whitelist);
            if (showFull)
                acceptableDenies.Add(ConnectionDenyReason.Full);
            if (showPanic)
                acceptableDenies.Add(ConnectionDenyReason.Panic);

            logQuery = logQuery.Where(c => acceptableDenies.Contains(c.Denied));

            SortState = await LoadSortConnectionsTableData(Pagination, _dbContext, logQuery, sort, AllRouteData);
        }

        [MustUseReturnValue]
        public static async Task<ISortState> LoadSortConnectionsTableData(
            PaginationState<Connection> pagination,
            PostgresServerDbContext dbContext,
            IQueryable<ConnectionLog> query,
            string? sort,
            Dictionary<string, string?> allRouteData)
        {
            var logs = query.LeftJoin(
                dbContext.Player,
                c => c.UserId, p => p.UserId,
                (c, p) => new { c, HitCount = c.BanHits.Count, Player = p });

            var sortState = Helpers.SortState.Build(logs);

            sortState.AddColumn("name", c => c.c.UserName);
            sortState.AddColumn("uid", c => c.c.UserId);
            sortState.AddColumn("time", c => c.c.Time, SortOrder.Descending);
            sortState.AddColumn("addr", c => c.c.Address);
            sortState.AddColumn("hwid", c => c.c.HWId);
            sortState.AddColumn("denied", c => c.c.Denied);
            sortState.AddColumn("hits", c => c.c.BanHits.Count);
            sortState.Init(sort, allRouteData);

            logs = sortState.ApplyToQuery(logs);

            await pagination.LoadLinqAsync(logs, e => e.Select(c => new Connection(c.c, c.HitCount, c.Player)));

            return sortState;
        }

        public sealed record Connection(
            ConnectionLog Log,
            int BanHitCount,
            Player? Player);
    }
}
