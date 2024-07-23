using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using Content.Server.Database;
using Content.Shared.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SS14.Admin.AdminLogs;
using SS14.Admin.Models;

namespace SS14.Admin.Pages.Logs;

public class LogsIndexModel : PageModel
{
        private readonly PostgresServerDbContext _dbContext;

        public List<AdminLog> Items { get; set; } = new();
        public Dictionary<string, string?> AllRouteData { get; } = new();

        [BindProperty(SupportsGet = true)]
        public DateTime FromDate { get; set; } = DateTime.UtcNow.Subtract(TimeSpan.FromDays(1));

        [BindProperty(SupportsGet = true)]
        public DateTime ToDate { get; set; } = DateTime.UtcNow;

        [BindProperty(SupportsGet = true)]
        public OrderColumn Sort { get; set; } = OrderColumn.Date;

        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 0;

        [BindProperty(SupportsGet = true)]
        public int PerPage { get; set; } = 100;

        public LogType? TypeSearch { get; set; } = null;

        [BindProperty(SupportsGet = true)]
        public int? SeveritySearch { get; set; }

        public List<string> PaginationOptions { get; set; } = new() { "100", "200", "300", "500" }; //I think this is enough per page, if needed I can add the ability for the user to add their own custom amount
        public Dictionary<int, string> SeverityOptions { get; set; } = new()
        {
            { -2, "Any" },
            { -1, "Low" },
            { 0, "Medium" },
            { 1, "High" },
            { 2, "Extreme" }
        };
        public string? ServerSearch { get; set; }
        public string? Search { get; set; }

        public LogsIndexModel(PostgresServerDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task OnGetAsync(
            string? daterange,
            string? search,
            int? roundId,
            string? player,
            string? type,
            int? severity,
            int? countselect
        )
        {
            //Converts "any" to null in order to correctly use FindAdminLogs()
            SeveritySearch = severity != -2 ? severity : null;

            var playerUserId = AdminLogRepository.FindPlayerByName(_dbContext.Player, player!).Result?.UserId.ToString();

            //if you you leave the type filter as "" it just defaults to uknown as the type witch fucking breaks everything
            TypeSearch = CreateTypeFromValue(type);

            if (countselect != null) PerPage = (int)countselect;

            //Add all search params to AllRouteData
            AllRouteData.Add("fromDate", FromDate.ToString("yyyy-MM-dd"));
            AllRouteData.Add("toDate", ToDate.ToString("yyyy-MM-dd"));
            AllRouteData.Add("search", search);
            AllRouteData.Add("roundId", roundId.ToString());
            AllRouteData.Add("player", player);
            AllRouteData.Add("type", type);
            AllRouteData.Add("severity", severity.ToString());
            AllRouteData.Add("countselect", countselect.ToString());

            Items = await AdminLogRepository.FindAdminLogs(
                _dbContext,
                _dbContext.AdminLog,
                playerUserId,
                FromDate,
                ToDate,
                ServerSearch,
                TypeSearch,
                search,
                roundId,
                SeveritySearch,
                Sort,
                PerPage,
                PageIndex * PerPage
            );
        }

        public LogType? CreateTypeFromValue(string? value)
        {
            if (value != null && Enum.TryParse(value, out LogType type))
            {
                return type;
            }
            return null;
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

