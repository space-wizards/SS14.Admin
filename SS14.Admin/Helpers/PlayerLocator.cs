using System.Net;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;

namespace SS14.Admin.Helpers;

/// <summary>
/// Helper class for resolving player names.
/// </summary>
public sealed class PlayerLocator
{
    private readonly PostgresServerDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public PlayerLocator(PostgresServerDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    /// <summary>
    /// Resolve a user-entered string into a user ID.
    /// </summary>
    /// <remarks>
    /// This will first check the local database, then ask the authentication server.
    /// If the input is a valid GUID, it is returned as-is.
    /// </remarks>
    /// <param name="input"></param>
    /// <returns>Null if the user could not be found.</returns>
    public async Task<Guid?> Resolve(string input)
    {
        input = input.Trim();
        // Attempt 1: parse as GUID.
        if (Guid.TryParse(input, out var guid))
            return guid;

        // Attempt 2: check game server database.
        var localPlayer = await _dbContext.Player
            .SingleOrDefaultAsync(p => EF.Functions.ILike(p.LastSeenUserName, input));

        if (localPlayer != null)
            return localPlayer.UserId;

        // Attempt 3: ask auth server.
        var server = _configuration["AuthServer"];
        var client = new HttpClient();

        var url = $"{server}/api/query/name?name={Uri.EscapeDataString(input)}";
        var resp = await client.GetAsync(url);
        if (resp.StatusCode != HttpStatusCode.NotFound)
        {
            resp.EnsureSuccessStatusCode();
            return (await resp.Content.ReadFromJsonAsync<QueryUserResponse>())!.UserId;
        }

        // Attempt 4: give up.
        return null;
    }

    private sealed record QueryUserResponse(
        string UserName,
        Guid UserId,
        string? PatronTier,
        DateTimeOffset CreatedTime);
}
