using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Server.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SS14.Admin.Helpers;
using SS14.Admin.Models;

namespace SS14.Admin.Pages.Logs
{
    public class LogsIndexModel : PageModel
    {
        private readonly PostgresServerDbContext _dbContext;
        public PaginationState<AdminLog> Pagination { get; } = new(100);
        public SortState<AdminLog> SortState { get; } = new();
        public Dictionary<string, string?> AllRouteData { get; } = new();

        [BindProperty(SupportsGet = true)]
        public DateTime FromDate { get; set; }
        [BindProperty(SupportsGet = true)]
        public DateTime ToDate { get; set; }
        [BindProperty(SupportsGet = true), ModelBinder(BinderType = typeof(JsonQueryBinder))]
        public List<AdminLogFilterModel>? Filters { get; set; }

        public LogsIndexModel(PostgresServerDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task OnGetAsync(string? sort)
        {
            AllRouteData.Add("fromDate", FromDate.ToString("yyyy-MM-dd"));
            AllRouteData.Add("toDate", ToDate.ToString("yyyy-MM-dd"));
            AllRouteData.Add("filters", JsonSerializer.Serialize(Filters, new JsonSerializerOptions(JsonSerializerDefaults.Web)));



            /*SortState.AddColumn("name", p => p.LastSeenUserName, SortOrder.Ascending);
            SortState.AddColumn("uid", p => p.UserId);
            SortState.AddColumn("last_seen_time", p => p.LastSeenTime);
            SortState.AddColumn("last_seen_addr", p => p.LastSeenAddress);
            SortState.AddColumn("first_seen", p => p.FirstSeenTime);*/
            SortState.AddColumn("date", entry => entry.Date, SortOrder.Descending);
            SortState.AddColumn("impact", entry => entry.Impact);
            SortState.AddColumn("message", entry => entry.Message);
            SortState.Init(sort, AllRouteData);

            Pagination.Init(0, 100, AllRouteData);

            IQueryable<AdminLog> logQuery = _dbContext.AdminLog;

            logQuery = ApplyDateFilter(logQuery, FromDate);
            logQuery = ApplyDateFilter(logQuery, ToDate, true);

            logQuery = SortState.ApplyToQuery(logQuery);
            await Pagination.LoadAsync(logQuery);
        }

        private static IQueryable<AdminLog> ApplyDateFilter(IQueryable<AdminLog> query, DateTime date, bool isEndDate = false)
        {
            if (date == default) return query;

            if (isEndDate)
                return query.Where(e => e.Date.CompareTo(date.AddHours(23)) <= 0);

            return query.Where(e => e.Date.CompareTo(date) >= 0);
        }
    }
}
