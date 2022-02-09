using SS14.Admin.Helpers;

namespace SS14.Admin.Pages.Tables;

public sealed record BansTableModel(ISortState SortState, PaginationState<BansModel.Ban> Pagination, int? HighlightBan);
