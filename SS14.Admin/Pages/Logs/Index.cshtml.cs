using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Shared.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SS14.Admin.AdminLogs;
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
        public int? RoundId { get; set; }

        [BindProperty(SupportsGet = true)]
        public OrderColumn Sort { get; set; } = OrderColumn.Date;

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

            //I will replace the filter tag stuff in my next PR so this is just temporary
            var playerUserId = Filters?.Find(tag => tag.Key == LogFilterTags.Player);
            var serverName = Filters?.Find(tag => tag.Key == LogFilterTags.Server);
            var type = Filters?.Find(tag => tag.Key == LogFilterTags.Type);
            var search = Filters?.Find(tag => tag.Key == LogFilterTags.Search);

            Enum.TryParse<LogType>(type?.Key.TransformValue(_dbContext, type.Value) ?? string.Empty, true, out var parsedTag);

            Items = await AdminLogRepository.FindAdminLogs(
                _dbContext,
                _dbContext.AdminLog,
                playerUserId?.Key.TransformValue(_dbContext, playerUserId.Value),
                FromDate,
                ToDate,
                serverName?.Value,
                type != null ? parsedTag : null,
                search?.Value,
                RoundId,
                Sort,
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

        public enum OrderColumn
        {
            Date,
            Impact
        }
    }
}
