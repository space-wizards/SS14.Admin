using Content.Shared.Database;

namespace SS14.Admin.AdminLogs
{
    public static class AdminLogImpactExtension
    {
        public static string CssColorVarName(this LogImpact impact) => impact switch
        {
            LogImpact.Low => "--color-severity-low",
            LogImpact.Medium => "--color-severity-medium",
            LogImpact.High => "--color-severity-high",
            LogImpact.Extreme => "--color-severity-extreme",
            _ => ""
        };
}
}
