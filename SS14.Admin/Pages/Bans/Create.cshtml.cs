using System.ComponentModel.DataAnnotations;
using Content.Server.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
            public int LengthMinutes { get; set; }
            [Required] public string Reason { get; set; } = "";
        }

        public async Task OnPostFillAsync()
        {
            if (string.IsNullOrWhiteSpace(Input.NameOrUid))
            {
                TempData.SetStatusError("Must provide name/UID.");
                return;
            }

            var lastInfo = await _banHelper.GetLastPlayerInfo(Input.NameOrUid);
            if (lastInfo == null)
            {
                TempData.SetStatusError("Unable to find player");
                return;
            }

            Input.IP = lastInfo.Value.address.ToString();
            Input.HWid = lastInfo.Value.hwid is { } h ? Convert.ToBase64String(h) : null;
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            var ban = new ServerBan();

            var error = await _banHelper.FillBanCommon(
                ban,
                Input.NameOrUid,
                Input.IP,
                Input.HWid,
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
    }
}
