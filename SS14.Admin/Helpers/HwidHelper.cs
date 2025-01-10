using Content.Server.Database;
using Content.Shared.Database;

namespace SS14.Admin.Helpers;

public static class HwidHelper
{
    public static ImmutableTypedHwid ToImmutable(this TypedHwid hwid)
    {
        // Easiest function of my life.
        return hwid;
    }
}
