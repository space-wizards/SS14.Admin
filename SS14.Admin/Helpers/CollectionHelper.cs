namespace SS14.Admin.Helpers
{
    public static class CollectionHelper
    {
        public static Dictionary<TKey, TValue> ShallowClone<TKey, TValue>(this IDictionary<TKey, TValue> dict)
            where TKey : notnull
        {
            return new(dict);
        }
    }
}