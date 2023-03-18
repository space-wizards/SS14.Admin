using SS14.Admin.Helpers;

namespace SS14.Admin.Pages.Tables
{
    public sealed record PaginationModel(IPaginatedList List, Dictionary<string, string?> AllRouteData);
}