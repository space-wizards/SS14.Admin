using System.ComponentModel.DataAnnotations;
using Content.Server.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Helpers;
using WhitelistEntity = Content.Server.Database.Whitelist;

namespace SS14.Admin.Pages.Whitelist;

public class AddWhitelist : PageModel
{
    private readonly PostgresServerDbContext _dbContext;
    private readonly PlayerLocator _playerLocator;

    [BindProperty] public InputModel Input { get; set; } = new();
    [TempData] public string? StatusMessage { get; set; }

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
            StatusMessage = "Error: no name given";
            return Page();
        }

        // Resolve UID from input.
        var uid = await _playerLocator.Resolve(Input.NameOrUid);
        if (uid == null)
        {
            StatusMessage = "Unable to find player";
            return Page();
        }

        var whitelist = await _dbContext.Whitelist.SingleOrDefaultAsync(w => w.UserId == uid);
        if (whitelist != null)
        {
            StatusMessage = "Player is already whitelisted";
            return Page();
        }

        _dbContext.Add(new WhitelistEntity
        {
            UserId = uid.Value
        });
        await _dbContext.SaveChangesAsync();

        TempData["HighlightNewWhitelist"] = uid;
        StatusMessage = "Successfully added player to whitelist";

        return RedirectToPage("./Index");
    }
}
