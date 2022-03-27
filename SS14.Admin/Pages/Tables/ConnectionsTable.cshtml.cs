using SS14.Admin.Helpers;
using SS14.Admin.Pages.Connections;

namespace SS14.Admin.Pages.Tables;

public sealed record ConnectionsTableModel(ISortState SortState, PaginationState<ConnectionsIndexModel.Connection> Pagination);
