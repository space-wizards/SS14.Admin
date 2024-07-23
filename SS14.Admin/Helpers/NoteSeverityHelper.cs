using Content.Shared.Database;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SS14.Admin.Helpers;

public static class NoteSeverityHelper
{
    public static SelectListItem[] SeverityItems =>
    [
        new SelectListItem("None", NoteSeverity.None.ToString()),
        new SelectListItem("Minor", NoteSeverity.Minor.ToString()),
        new SelectListItem("Medium", NoteSeverity.Medium.ToString()),
        new SelectListItem("High", NoteSeverity.High.ToString()),
    ];
}
