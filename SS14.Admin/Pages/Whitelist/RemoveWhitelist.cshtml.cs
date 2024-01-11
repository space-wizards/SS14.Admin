using Content.Server.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WhitelistEntity = Content.Server.Database.Whitelist;

namespace SS14.Admin.Pages.Whitelist;

public class RemoveWhitelist : PageModel
{
    private readonly PostgresServerDbContext _dbContext;

    public WhitelistEntity Whitelist = default!;
    public Player? Player;

    [TempData] public string? StatusMessage { get; set; }

    public RemoveWhitelist(PostgresServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> OnGetAsync(Guid userId)
    {
        var whitelist = await _dbContext.Whitelist.SingleOrDefaultAsync(w => w.UserId == userId);
        if (whitelist == null)
            return NotFound();

        Whitelist = whitelist;

        Player = await _dbContext.Player.SingleOrDefaultAsync(w => w.UserId == userId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid userId)
    {
        var whitelist = await _dbContext.Whitelist.SingleOrDefaultAsync(w => w.UserId == userId);
        if (whitelist == null)
            return NotFound();

        _dbContext.Remove(whitelist);
        await _dbContext.SaveChangesAsync();

        StatusMessage = "Successfully removed whitelist";
        return RedirectToPage("./Index");
    }
}
