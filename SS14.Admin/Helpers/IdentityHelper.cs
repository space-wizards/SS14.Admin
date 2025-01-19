using System.Security.Claims;

namespace SS14.Admin.Helpers
{
    public static class IdentityHelper
    {
        public static Guid GetUserId(this IEnumerable<Claim> claims)
        {
            return new Guid(claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        }
    }
}
