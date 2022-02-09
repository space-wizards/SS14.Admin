using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Database;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SS14.Admin.Helpers;

namespace SS14.Admin.Pages.Connections
{
    public class ConnectionsIndexModel : PageModel
    {
        private readonly PostgresServerDbContext _dbContext;

        public SortState<ConnectionLog> SortState { get; } = new();
        public PaginationState<ConnectionLog> Pagination { get; } = new(100);
        public Dictionary<string, string?> AllRouteData { get; } = new();

        public string? CurrentFilter { get; set; }
        public bool ShowAccepted { get; set; }
        public bool ShowBanned { get; set; }
        public bool ShowWhitelist { get; set; }
        public bool ShowFull { get; set; }

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
            bool showFull)
        {
            SortState.AddColumn("name", c => c.UserName);
            SortState.AddColumn("uid", c => c.UserId);
            SortState.AddColumn("time", c => c.Time, SortOrder.Descending);
            SortState.AddColumn("addr", c => c.Address);
            SortState.AddColumn("hwid", c => c.HWId);
            SortState.AddColumn("denied", c => c.Denied);
            SortState.Init(sort, AllRouteData);

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
            }

            CurrentFilter = search;
            ShowAccepted = showAccepted;
            ShowBanned = showBanned;
            ShowWhitelist = showWhitelist;
            ShowFull = showFull;

            AllRouteData.Add("search", CurrentFilter);
            AllRouteData.Add("showAccepted", showAccepted.ToString());
            AllRouteData.Add("showBanned", showBanned.ToString());
            AllRouteData.Add("showWhitelist", showWhitelist.ToString());
            AllRouteData.Add("showFull", showFull.ToString());
            AllRouteData.Add("showSet", "true");

            IQueryable<ConnectionLog> logQuery = _dbContext.ConnectionLog;
            logQuery = SearchHelper.SearchConnectionLog(logQuery, search);

            var acceptableDenies = new List<ConnectionDenyReason?>();
            if (showAccepted)
                acceptableDenies.Add(null);
            if (showBanned)
                acceptableDenies.Add(ConnectionDenyReason.Ban);
            if (showWhitelist)
                acceptableDenies.Add(ConnectionDenyReason.Whitelist);
            if (showFull)
                acceptableDenies.Add(ConnectionDenyReason.Full);

            logQuery = logQuery.Where(c => acceptableDenies.Contains(c.Denied));

            var sortedQuery = SortState.ApplyToQuery(logQuery).ThenByDescending(s => s.Time);

            await Pagination.LoadAsync(sortedQuery);
        }
    }
}
