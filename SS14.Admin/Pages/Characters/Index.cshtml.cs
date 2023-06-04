using Content.Server.Database;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Helpers;

namespace SS14.Admin.Pages.Characters;

public sealed class CharactersIndexModel : PageModel
{
    private readonly PostgresServerDbContext _dbContext;

    public PaginationState<CharacterPlayerData> Pagination { get; } = new(100);
    public SortState<CharacterPlayerData> SortState { get; } = new();
    public Dictionary<string, string?> AllRouteData { get; } = new();

    public string? CurrentFilter { get; set; } = "";

    public CharactersIndexModel(PostgresServerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task OnGetAsync(
        string? sort,
        string? search,
        int? pageIndex,
        int? perPage)
    {
        SortState.AddColumn("player_name", p => p.Player!.LastSeenUserName, SortOrder.Ascending);
        SortState.AddColumn("character_name", p => p.Profile.CharacterName);
        SortState.Init(sort, AllRouteData);

        Pagination.Init(pageIndex, perPage, AllRouteData);

        CurrentFilter = search?.Trim();
        AllRouteData.Add("search", CurrentFilter);

        var profileQuery = _dbContext.Profile.LeftJoin(
            _dbContext.Player,
            p => p.Preference.UserId, pl => pl.UserId,
            (p, pl) => new CharacterPlayerData { Profile = p, Player = pl }
        );

        if (!string.IsNullOrWhiteSpace(CurrentFilter))
        {
            profileQuery = profileQuery.Where(u => EF.Functions.ILike(u.Profile.CharacterName, $"%{CurrentFilter}%"));
        }

        profileQuery = SortState.ApplyToQuery(profileQuery);

        await Pagination.LoadAsync(profileQuery);
    }

    public sealed class CharacterPlayerData
    {
        public required Profile Profile { get; init; }
        public required Player? Player { get; init; }

        public void Deconstruct(out Profile profile, out Player? player)
        {
            profile = Profile;
            player = Player;
        }
    }
}
