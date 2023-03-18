using System.Security.Claims;
using Content.Server.Database;

namespace SS14.Admin.SignIn
{
    public sealed class SignInManager
    {
        private readonly PostgresServerDbContext _dbContext;

        public SignInManager(PostgresServerDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public SignInData? GetSignInData(ClaimsPrincipal principal)
        {
            if (!principal.Identities.Any(i => i.IsAuthenticated))
                return null;

            return new SignInData(principal.Claims.Single(c => c.Type == "name").Value, principal.IsInRole("ADMIN"));
        }
    }
}
