using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Content.Server.Database;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SS14.Admin.Admins;

namespace SS14.Admin
{
    public sealed class LoginHandler
    {
        private readonly PostgresServerDbContext _dbContext;
        private readonly LinkGenerator _linkGenerator;

        public LoginHandler(
            PostgresServerDbContext dbContext,
            LinkGenerator linkGenerator)
        {
            _dbContext = dbContext;
            _linkGenerator = linkGenerator;
        }

        public async Task HandleTokenValidated(TokenValidatedContext ctx)
        {
            var identity = ctx.Principal?.Identities?.FirstOrDefault(i => i.IsAuthenticated);
            if (identity == null)
            {
                Debug.Fail("Unable to find identity.");
            }

            var sub = identity.Claims.Single(c => c.Type == "sub").Value;
            var guid = Guid.Parse(sub);

            var adminData = await _dbContext.Admin
                .Include(a => a.AdminRank)
                .ThenInclude(r => r!.Flags)
                .Include(a => a.Flags)
                .AsSplitQuery()
                .FirstOrDefaultAsync(a => a.UserId == guid);

            if (adminData == null)
            {
                ctx.Response.Redirect(_linkGenerator.GetUriByPage(ctx.HttpContext, "/LoginFailed"));

                ctx.HandleResponse();
                return;
            }

            var flags = AdminHelper.GetStringFlags(adminData);

            foreach (var flag in flags)
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, flag));
            }
        }
    }
}