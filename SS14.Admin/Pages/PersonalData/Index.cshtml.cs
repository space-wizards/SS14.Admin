using System.Net.Mime;
using Content.Server.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Helpers;
using SS14.Admin.PersonalData;

namespace SS14.Admin.Pages.PersonalData;

public sealed class Index(PostgresServerDbContext dbContext, PersonalDataDownloader downloader) : PageModel
{
    public bool IsMigrationCompatible { get; set; }

    public async Task OnGetAsync()
    {
        IsMigrationCompatible = await downloader.GetIsMigrationCompatible();
    }

    public async Task<IActionResult> OnPostDownloadPersonalDataAsync(
        [FromForm] DownloadPersonalDataRequest request,
        CancellationToken cancel)
    {
        var name = (request.Name ?? "").Trim();
        var player = await dbContext.Player.SingleOrDefaultAsync(
            player => player.LastSeenUserName == request.Name,
            cancellationToken: cancel);
        if (player == null)
        {
            TempData.SetStatusError($"Unable to find player: '{name}'");
            return RedirectToPage();
        }

        var stream = new MemoryStream();
        await downloader.CollectPersonalData(player.UserId, stream, cancel);
        stream.Position = 0;

        return File(stream, MediaTypeNames.Application.Zip, $"{name}_game_personal_data.zip");
    }

    public sealed class DownloadPersonalDataRequest
    {
        public string? Name { get; set; }
    }
}
