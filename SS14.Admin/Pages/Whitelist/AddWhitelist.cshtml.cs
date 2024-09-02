using System.ComponentModel.DataAnnotations;
using Content.Server.Database;
using Content.Shared.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Helpers;
using WhitelistEntity = Content.Server.Database.Whitelist;

namespace SS14.Admin.Pages.Whitelist;

public class AddWhitelist : PageModel
{
    private readonly PostgresServerDbContext _dbContext;
    private readonly PlayerLocator _playerLocator;

    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required] public string? NameOrUid { get; set; }
    }

    public AddWhitelist(PostgresServerDbContext dbContext, PlayerLocator playerLocator)
    {
        _dbContext = dbContext;
        _playerLocator = playerLocator;
    }

    public IActionResult OnGetAsync(string? name)
    {
        Input.NameOrUid ??= name;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Input.NameOrUid))
        {
            TempData.SetStatusError("No name given");
            return Page();
        }

        // Resolve UID from input.
        var uid = await _playerLocator.Resolve(Input.NameOrUid);
        if (uid == null)
        {
            TempData.SetStatusError("Unable to find player");
            return Page();
        }

        var whitelist = await _dbContext.Whitelist.SingleOrDefaultAsync(w => w.UserId == uid);
        if (whitelist != null)
        {
            TempData.SetStatusError("Player is already whitelisted");
            return Page();
        }

        _dbContext.Add(new WhitelistEntity
        {
            UserId = uid.Value
        });
        var whitelisterUid = User.Claims.GetUserId();
        var playerName = await AuditHelper.GetNameFromUidOrDefault(_dbContext, uid);
        await AuditHelper.AddUnsavedLogAsync(_dbContext, AuditLogType.Whitelist, LogImpact.Medium, whitelisterUid,
            $"{playerName} was whitelisted", [uid.Value]);
        await _dbContext.SaveChangesAsync();

        TempData["HighlightNewWhitelist"] = uid;
        TempData.SetStatusInformation($"Successfully added {playerName} to the whitelist");

        return RedirectToPage("./Index");
    }
}
