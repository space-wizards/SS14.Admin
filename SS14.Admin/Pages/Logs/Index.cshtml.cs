using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Server.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.AdminLogs;
using SS14.Admin.Helpers;
using SS14.Admin.Models;

namespace SS14.Admin.Pages.Logs
{
    public class LogsIndexModel : PageModel
    {
        private readonly PostgresServerDbContext _dbContext;

        public List<AdminLog> Items { get; set; } = new();
        public Dictionary<string, string?> AllRouteData { get; } = new();

        [BindProperty(SupportsGet = true)]
        public DateTime FromDate { get; set; } = DateTime.Now.Subtract(TimeSpan.FromDays(1));

        [BindProperty(SupportsGet = true)]
        public DateTime ToDate { get; set; } = DateTime.Now;

        [BindProperty(SupportsGet = true), ModelBinder(BinderType = typeof(JsonQueryBinder))]
        public List<AdminLogFilterModel>? Filters { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 0;

        [BindProperty(SupportsGet = true)]
        public int PerPage { get; set; } = 100;

        public LogsIndexModel(PostgresServerDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task OnGetAsync()
        {
            AllRouteData.Add("fromDate", FromDate.ToString("yyyy-MM-dd"));
            AllRouteData.Add("toDate", ToDate.ToString("yyyy-MM-dd"));
            AllRouteData.Add("filters", JsonSerializer.Serialize(Filters, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

            Items = AdminLogRepository.List(
                _dbContext,
                _dbContext.AdminLog,
                Filters ?? new List<AdminLogFilterModel>(),
                FromDate,
                ToDate,
                PerPage,
                PageIndex * PerPage
            );
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
