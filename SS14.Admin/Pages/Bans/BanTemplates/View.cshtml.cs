using System.ComponentModel.DataAnnotations;
using Content.Server.Database;
using Content.Shared.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Helpers;

namespace SS14.Admin.Pages.Bans.BanTemplates;

[Authorize(Roles = "BAN")]
[ValidateAntiForgeryToken]
public class View(PostgresServerDbContext dbContext) : PageModel
{
    public BanTemplate Template = default!;

    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required] public string Title { get; set; } = "";
        public int LengthMinutes { get; set; }
        public string? Reason { get; set; }

        [Display(Name = "Delete ban when expired")]
        public bool AutoDelete { get; set; }

        [Display(Name = "Hidden from player")] public bool Hidden { get; set; }
        public NoteSeverity Severity { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var template = await dbContext.BanTemplate.SingleOrDefaultAsync(t => t.Id == id);
        if (template == null)
            return NotFound();

        Input.Hidden = template.Hidden;
        Input.Severity = template.Severity;
        Input.Reason = template.Reason;
        Input.Title = template.Title;
        Input.LengthMinutes = (int)template.Length.TotalMinutes;
        Input.AutoDelete = template.AutoDelete;

        Template = template;
        return Page();
    }

    public async Task<IActionResult> OnPostEditAsync(int id)
    {
        if (!ModelState.IsValid)
            return RedirectToPage(new { id });

        var template = await dbContext.BanTemplate.SingleOrDefaultAsync(t => t.Id == id);
        if (template == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(Input.Title))
        {
            TempData["StatusMessage"] = "Error: title is empty";
            return RedirectToPage(new { id });
        }

        template.Title = Input.Title;
        template.Length = TimeSpan.FromMinutes(Input.LengthMinutes);
        template.Reason = Input.Reason ?? "";
        template.AutoDelete = Input.AutoDelete;
        template.Hidden = Input.Hidden;
        template.Severity = Input.Severity;

        var flags = ServerBanExemptFlags.None;
        foreach (var (value, _) in BanExemptions.GetExemptions())
        {
            if (Request.Form.TryGetValue($"exemption_{value}", out var checkValue) && checkValue == "on")
            {
                flags |= value;
            }
        }

        template.ExemptFlags = flags;

        await dbContext.SaveChangesAsync();

        TempData.SetStatusInformation("Changes saved");

        return RedirectToPage(new { id });
    }
}
