using SS14.Admin.Helpers;
using Index = SS14.Admin.Pages.RoleBans.Index;

namespace SS14.Admin.Pages.Tables;

public sealed record RoleBansTableModel(
    ISortState SortState,
    PaginationState<Index.RoleBan> Pagination,
    int? HighlightBan);
