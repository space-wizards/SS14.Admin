using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace SS14.Admin.Helpers;

public static class AuthorizationHelper
{
    public static async Task<bool> IsAuthorizedForAsync(
        this IAuthorizationService service,
        ClaimsPrincipal user,
        string policyName)
    {
        var result = await service.AuthorizeAsync(user, null, policyName);
        return result.Succeeded;
    }
}
