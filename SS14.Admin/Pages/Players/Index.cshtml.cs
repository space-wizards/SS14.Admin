using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Content.Server.Database;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Helpers;

namespace SS14.Admin.Pages.Players
{
    public class PlayersIndexModel : PageModel
    {
        private readonly PostgresServerDbContext _dbContext;

        public PaginationState<Player> Pagination { get; } = new(100);
        public SortState<Player> SortState { get; } = new();
        public Dictionary<string, string?> AllRouteData { get; } = new();

        public string? CurrentFilter { get; set; } = "";


        public PlayersIndexModel(PostgresServerDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task OnGetAsync(
            string? sort,
            string? search,
            int? pageIndex,
            int? perPage)
        {
            SortState.AddColumn("name", p => p.LastSeenUserName, SortOrder.Ascending);
            SortState.AddColumn("uid", p => p.UserId);
            SortState.AddColumn("last_seen_time", p => p.LastSeenTime);
            SortState.AddColumn("last_seen_addr", p => p.LastSeenAddress);
            SortState.AddColumn("last_seen_hwid", p => p.LastSeenHWId);
            SortState.AddColumn("first_seen", p => p.FirstSeenTime);
            SortState.Init(sort, AllRouteData);

            Pagination.Init(pageIndex, perPage, AllRouteData);

            CurrentFilter = search;
            AllRouteData.Add("search", CurrentFilter);

            IQueryable<Player> userQuery = _dbContext.Player;
            if (!string.IsNullOrEmpty(search))
            {
                if (Guid.TryParse(search, out var guid))
                {
                    userQuery = userQuery.Where(u => u.UserId == guid);
                }
                else if (IPHelper.TryParseCidr(search, out var cidr))
                {
                    userQuery = userQuery.Where(u => EF.Functions.Contains(cidr, u.LastSeenAddress));
                }
                else if (IPAddress.TryParse(search, out var ip))
                {
                    userQuery = userQuery.Where(u => u.LastSeenAddress.Equals(ip));
                }
                else
                {
                    var normalized = search.ToUpperInvariant();
                    userQuery = userQuery.Where(u => u.LastSeenUserName.ToUpper().Contains(normalized));
                }
            }

            userQuery = SortState.ApplyToQuery(userQuery);

            await Pagination.LoadAsync(userQuery);
        }
    }
}
