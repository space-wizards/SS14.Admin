using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Content.Server.Database;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Helpers;

namespace SS14.Admin.Pages.Connections
{
    public class ConnectionsIndexModel : PageModel
    {
        private readonly PostgresServerDbContext _dbContext;
        
        public SortState<PostgresConnectionLog> SortState { get; } = new();
        public PaginationState<PostgresConnectionLog> Pagination { get; } = new(100);
        public Dictionary<string, string?> AllRouteData { get; } = new();

        public string? CurrentFilter { get; set; }

        public ConnectionsIndexModel(PostgresServerDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task OnGetAsync(
            string? sort,
            string? search,
            int? pageIndex,
            int? perPage)
        {
            SortState.AddColumn("name", c => c.UserName);
            SortState.AddColumn("uid", c => c.UserId);
            SortState.AddColumn("time", c => c.Time, SortOrder.Descending);
            SortState.AddColumn("addr", c => c.Address);
            SortState.Init(sort, AllRouteData);
            
            Pagination.Init(pageIndex, perPage, AllRouteData);
            
            CurrentFilter = search;
            AllRouteData.Add("search", CurrentFilter);

            IQueryable<PostgresConnectionLog> logQuery = _dbContext.ConnectionLog;
            if (!string.IsNullOrEmpty(search))
            {
                if (Guid.TryParse(search, out var guid))
                {
                    logQuery = logQuery.Where(u => u.UserId == guid);
                }
                else if (IPHelper.TryParseCidr(search, out var cidr))
                {
                    logQuery = logQuery.Where(u => EF.Functions.Contains(cidr, u.Address));
                }
                else if (IPAddress.TryParse(search, out var ip))
                {
                    logQuery = logQuery.Where(u => u.Address.Equals(ip));
                }
                else
                {
                    var normalized = search.ToUpperInvariant();
                    logQuery = logQuery.Where(u => u.UserName.ToUpper().Contains(normalized));
                }
            }

            var sortedQuery = SortState.ApplyToQuery(logQuery).ThenByDescending(s => s.Time);

            await Pagination.LoadAsync(sortedQuery);
        }
    }
}