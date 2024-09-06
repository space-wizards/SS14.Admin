using Content.Server.Database;
using Content.Shared.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SS14.Admin.AdminLogs;
using SS14.Admin.AuditLogs;

namespace SS14.Admin.Pages.AuditLogs;

[Authorize(Roles = "AUDIT")]
[ValidateAntiForgeryToken]
public class AuditLogsIndexModel(PostgresServerDbContext dbContext) : PageModel
{
        public List<AuditLog> Items { get; set; } = new();
        public Dictionary<string, string?> AllRouteData { get; } = new();

        [BindProperty(SupportsGet = true)]
        public DateTime FromDate { get; set; } = DateTime.UtcNow.Subtract(TimeSpan.FromDays(7));

        [BindProperty(SupportsGet = true)]
        public DateTime ToDate { get; set; } = DateTime.UtcNow;

        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 0;

        [BindProperty(SupportsGet = true)]
        public int PerPage { get; set; } = 100;

        public AuditLogType? TypeSearch { get; set; } = null;

        [BindProperty(SupportsGet = true)]
        public int? SeveritySearch { get; set; }

        public List<string> PaginationOptions { get; set; } = new() { "100", "200", "300", "500" };
        public Dictionary<int, string> SeverityOptions { get; set; } = new()
        {
            { -2, "Any" },
            { -1, "Low" },
            { 0, "Medium" },
            { 1, "High" },
            { 2, "Extreme" }
        };
        //public string? Search { get; set; }

        public async Task OnGetAsync(
            string? daterange,
            string? messageSearch,
            string? author,
            string? effected,
            string? type,
            int? severity,
            int? countselect
        )
        {
            //Converts "any" to null in order to correctly use FindAuditLogs()
            SeveritySearch = severity != -2 ? severity : null;

            var authorUid = author != null ? AdminLogRepository.FindPlayerByName(dbContext.Player, author).Result?.UserId : null;
            var effectedUid = effected != null ? AdminLogRepository.FindPlayerByName(dbContext.Player, effected).Result?.UserId : null;

            TypeSearch = CreateTypeFromValue(type);

            if (countselect != null) PerPage = (int)countselect;

            ////Add all search params to AllRouteData
            AllRouteData.Add("fromDate", FromDate.ToString("yyyy-MM-dd"));
            AllRouteData.Add("toDate", ToDate.ToString("yyyy-MM-dd"));
            AllRouteData.Add("messageSearch", messageSearch);
            AllRouteData.Add("author", author);
            AllRouteData.Add("effected", effected);
            AllRouteData.Add("type", type);
            AllRouteData.Add("severity", severity.ToString());
            AllRouteData.Add("countselect", countselect.ToString());

            Items = await AuditLogRepository.FindAuditLogs(
                context: dbContext,
                authorUserId: authorUid,
                effectedUserId: effectedUid,
                fromDate: FromDate, toDate: ToDate,
                type: TypeSearch,
                textSearch: messageSearch,
                severity: SeveritySearch,
                limit: PerPage,
                offset: PerPage * PageIndex);
        }

        public AuditLogType? CreateTypeFromValue(string? value)
        {
            if (value != null && Enum.TryParse(value, out AuditLogType type))
            {
                return type;
            }
            return null;
        }

        public async Task<string> RetrievePlayerLink(Guid uid)
        {
            var player = dbContext.Player.FirstOrDefault(p => p.UserId.Equals(uid));
            return player == default ? string.Empty : $"<a class=\"log-player-link\" href=\"/Players/Info/{player.UserId}\">{player.LastSeenUserName}</a>";
        }
    }

