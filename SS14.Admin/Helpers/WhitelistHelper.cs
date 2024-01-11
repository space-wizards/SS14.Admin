using Content.Server.Database;

namespace SS14.Admin.Helpers;

/// <summary>
/// Helper functions for working with whitelist entities.
/// </summary>
/// <seealso cref="Whitelist"/>
public static class WhitelistHelper
{
    /// <summary>
    /// Make a query that joins the whitelist table with the player table,
    /// if the player is known in the database.
    /// </summary>
    public static IQueryable<WhitelistJoin> MakeWhitelistJoin(PostgresServerDbContext dbContext)
    {
        var player = dbContext.Player;
        var whitelist = dbContext.Whitelist;

        return whitelist.LeftJoin(
            player,
            w => w.UserId,
            p => p.UserId,
            (w, p) => new WhitelistJoin { Whitelist = w, Player = p });
    }

    public sealed class WhitelistJoin
    {
        public required Whitelist Whitelist { get; set; }
        public Player? Player { get; set; }

        public void Deconstruct(out Whitelist whitelist, out Player? player)
        {
            whitelist = Whitelist;
            player = Player;
        }
    };
}
