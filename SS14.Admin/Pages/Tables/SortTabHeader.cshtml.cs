using SS14.Admin.Helpers;

namespace SS14.Admin.Pages.Tables
{
    public sealed record SortTabHeaderModel(ISortState State, string Column, string Text);
}
