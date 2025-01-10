using System.ComponentModel.DataAnnotations;
using Content.Server.Database;
using Content.Shared.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Helpers;

namespace SS14.Admin.Pages.Bans
{
    [Authorize(Roles = "BAN")]
    [ValidateAntiForgeryToken]
    public class Create : PageModel
    {
        private readonly PostgresServerDbContext _dbContext;
        private readonly BanHelper _banHelper;

        [BindProperty] public InputModel Input { get; set; } = new();

        public ServerBanExemptFlags ExemptFlags;

        public List<BanTemplate> Templates = [];

        public Create(PostgresServerDbContext dbContext, BanHelper banHelper)
        {
            _dbContext = dbContext;
            _banHelper = banHelper;
        }

        public sealed class InputModel
        {
            public string? NameOrUid { get; set; }
            public string? IP { get; set; }
            public string? HWid { get; set; }
            public bool UseLatestIp {get; set; }
            public bool UseLatestHwid { get; set; }
            public int LengthMinutes { get; set; }
            [Required] public string Reason { get; set; } = "";
            // [Display(Name = "Delete ban when expired")]
            // public bool AutoDelete { get; set; }

            [Display(Name = "Hidden from player")] public bool Hidden { get; set; }
            public NoteSeverity Severity { get; set; }
        }

        public async Task OnGetAsync()
        {
            await LoadTemplates();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            await LoadTemplates();

            ExemptFlags = BanExemptions.GetExemptionFromForm(Request.Form);

            var ban = new ServerBan();

            var ipAddr = Input.IP;
            var hwid = Input.HWid;

            ban.ExemptFlags = ExemptFlags;
            // ban.AutoDelete = Input.AutoDelete;
            ban.Hidden = Input.Hidden;
            ban.Severity = Input.Severity;

            if (Input.UseLatestHwid || Input.UseLatestIp)
            {
                if (string.IsNullOrWhiteSpace(Input.NameOrUid))
                {
                    TempData.SetStatusError("Must provide name/UID.");
                    return Page();
                }

                var lastInfo = await _banHelper.GetLastPlayerInfo(Input.NameOrUid);
                if (lastInfo == null)
                {
                    TempData.SetStatusError("Unable to find player");
                    return Page();
                }

                ipAddr = Input.UseLatestIp ? lastInfo.Value.address.ToString() : Input.IP;
                hwid = Input.UseLatestHwid ? lastInfo.Value.hwid?.ToString() : Input.HWid;
            }

            var error = await _banHelper.FillBanCommon(
                ban,
                Input.NameOrUid,
                ipAddr,
                hwid,
                Input.LengthMinutes,
                Input.Reason);

            if (error != null)
            {
                TempData.SetStatusError(error);
                return Page();
            }

            _dbContext.Ban.Add(ban);
            await _dbContext.SaveChangesAsync();
            TempData["HighlightNewBan"] = ban.Id;
            TempData["StatusMessage"] = "Ban created";
            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostUseTemplateAsync(int templateId)
        {
            await LoadTemplates();

            var template = await _dbContext.BanTemplate.SingleOrDefaultAsync(t => t.Id == templateId);
            if (template == null)
            {
                TempData.SetStatusError("That template does not exist!");
                return Page();
            }

            // Avoid errors causing params to not be passed through.
            ModelState.Clear();

            Input.Reason = template.Reason;
            Input.LengthMinutes = (int)template.Length.TotalMinutes;
            Input.Severity = template.Severity;
            // Input.AutoDelete = template.AutoDelete;
            Input.Hidden = template.Hidden;
            ExemptFlags = template.ExemptFlags;

            return Page();
        }

        private async Task LoadTemplates()
        {
            Templates = await _dbContext.BanTemplate.ToListAsync();
        }
    }
}
