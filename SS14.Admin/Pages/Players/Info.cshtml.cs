using Content.Server.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace SS14.Admin.Pages.Players;

public sealed class Info : PageModel
{
    private readonly PostgresServerDbContext _dbContext;

    public Player Player { get; set; } = default!;

    public AdminNote[] Notes { get; set; } = default!;
    public PlayTime[] PlayTimes { get; set; } = default!;
    public Profile[] Profiles { get; set; } = default!;

    public Info(PostgresServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> OnGetAsync(Guid userId)
    {
        var player = await _dbContext.Player.SingleOrDefaultAsync(a => a.UserId == userId);
        if (player == null)
            return NotFound();

        Player = player;

        Notes = await _dbContext.AdminNotes
            .Where(n => n.PlayerUserId == userId)
            .Include(n => n.CreatedBy)
            .Include(n => n.LastEditedBy)
            .OrderByDescending(n => n.CreatedAt)
            .ToArrayAsync();

        PlayTimes = await _dbContext.PlayTime
            .Where(t => t.PlayerId == userId)
            .ToArrayAsync();

        Profiles = await _dbContext.Profile
            .Where(t => t.Preference.UserId == userId)
            .OrderBy(t => t.Slot)
            .Include(t => t.Jobs)
            .ToArrayAsync();

        return Page();
    }
}
