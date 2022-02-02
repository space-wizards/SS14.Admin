using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Content.Server.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SS14.Admin.Helpers;

namespace SS14.Admin.Pages.Bans
{
    [Authorize(Roles = "BAN")]
    [ValidateAntiForgeryToken]
    public class Create : PageModel
    {
        private readonly PostgresServerDbContext _dbContext;
        private readonly IConfiguration _cfg;
        private readonly ILogger<Create> _log;

        [BindProperty] public InputModel Input { get; set; } = new();
        [TempData] public string? StatusMessage { get; set; }

        public Create(PostgresServerDbContext dbContext, IConfiguration cfg, ILogger<Create> log)
        {
            _dbContext = dbContext;
            _cfg = cfg;
            _log = log;
        }

        public sealed class InputModel
        {
            public string? NameOrUid { get; set; }
            public string? IP { get; set; }
            public int LengthMinutes { get; set; }
            [Required] public string Reason { get; set; } = "";
        }

        public async Task OnPostFillAsync()
        {
            if (string.IsNullOrWhiteSpace(Input.NameOrUid))
            {
                StatusMessage = "Error: Must provide name/UID.";
                return;
            }

            Player? player;
            if (Guid.TryParse(Input.NameOrUid, out var guid))
            {
                player = await _dbContext.Player.SingleOrDefaultAsync(p => p.UserId == guid);
            }
            else
            {
                player = await _dbContext.Player
                    .OrderByDescending(p => p.LastSeenTime)
                    .FirstOrDefaultAsync(p =>
                        p.LastSeenUserName.ToUpper() == Input.NameOrUid.ToUpper());
            }

            if (player == null)
            {
                StatusMessage = "Unable to find player";
            }
            else
            {
                Input.IP = player.LastSeenAddress.ToString();
            }
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (string.IsNullOrWhiteSpace(Input.NameOrUid) && string.IsNullOrWhiteSpace(Input.IP))
            {
                StatusMessage = "Error: Must provide at least one of name/UID and IP address.";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Input.Reason))
            {
                StatusMessage = "Error: Must provide reason.";
                return Page();
            }

            Guid? userId = null;
            (IPAddress, int)? addr = null;
            DateTime? expires = null;
            if (!string.IsNullOrWhiteSpace(Input.NameOrUid))
            {
                if (Guid.TryParse(Input.NameOrUid, out var guid))
                {
                    userId = guid;
                }
                else
                {
                    try
                    {
                        userId = await FindPlayerGuidByNameAsync(Input.NameOrUid);
                    }
                    catch (Exception e)
                    {
                        _log.LogError(e, "Unable to fetch user ID from auth server");
                        StatusMessage = "Error: Unknown error occured fetching user ID from auth server.";
                        return Page();
                    }

                    if (userId == null)
                    {
                        StatusMessage = $"Error: Unable to find user with name {Input.NameOrUid}";
                        return Page();
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(Input.IP))
            {
                if (!IPHelper.TryParseIpOrCidr(Input.IP, out var parsedAddr))
                {
                    StatusMessage = "Error: Invalid IP address/CIDR range";
                    return Page();
                }

                addr = parsedAddr;
            }

            if (Input.LengthMinutes != 0)
            {
                expires = DateTime.UtcNow + TimeSpan.FromMinutes(Input.LengthMinutes);
            }

            var admin = User.Claims.GetUserId();

            var ban = new ServerBan
            {
                UserId = userId,
                Address = addr,
                Reason = Input.Reason,
                ExpirationTime = expires,
                BanTime = DateTime.UtcNow,
                BanningAdmin = admin
            };

            _dbContext.Ban.Add(ban);
            await _dbContext.SaveChangesAsync();
            TempData["HighlightNewBan"] = ban.Id;
            TempData["StatusMessage"] = "Ban created";
            return RedirectToPage("./Index");
        }

        private async Task<Guid?> FindPlayerGuidByNameAsync(string name)
        {
            // Try our own database first, in case this is a guest or something.

            var player = await _dbContext.Player
                .Where(p => p.LastSeenUserName == name)
                .OrderByDescending(p => p.LastSeenTime)
                .FirstOrDefaultAsync();

            if (player != null)
                return player.UserId;

            var server = _cfg["AuthServer"];
            var client = new HttpClient();

            var url = $"{server}/api/query/name?name={Uri.EscapeDataString(name)}";

            var resp = await client.GetAsync(url);
            if (resp.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            resp.EnsureSuccessStatusCode();

            return (await resp.Content.ReadFromJsonAsync<QueryUserResponse>())!.UserId;
        }

        public sealed record QueryUserResponse(
            string UserName,
            Guid UserId,
            string? PatronTier,
            DateTimeOffset CreatedTime);
    }
}
