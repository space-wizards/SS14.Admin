using DBAdmin = Content.Server.Database.Admin;

namespace SS14.Admin.Admins
{
    public class AdminHelper
    {
        public static AdminFlags GetFlags(DBAdmin admin)
        {
            return AdminFlagsHelper.NamesToFlags(GetStringFlags(admin));
        }

        public static IEnumerable<string> GetStringFlags(DBAdmin admin)
        {
            var rankFlags = admin.AdminRank?.Flags.Select(f => f.Flag) ?? Enumerable.Empty<string>();
            var flagsPos = admin.Flags.Where(f => !f.Negative).Select(f => f.Flag);
            var flagsNeg = admin.Flags.Where(f => f.Negative).Select(f => f.Flag);

            return rankFlags.Union(flagsPos).Except(flagsNeg);
        }
    }
}