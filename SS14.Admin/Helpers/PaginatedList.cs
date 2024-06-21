using Microsoft.EntityFrameworkCore;

namespace SS14.Admin.Helpers
{
    public interface IPaginatedList
    {
        int? TotalCount { get; }
        int PageIndex { get; }
        int PageSize { get; }
        int? PageCount { get; }
        bool HasNextPage { get; }
        bool HasPrevPage { get; }
    }

    public sealed class PaginatedList<T> : IPaginatedList
    {
        public T[] PaginatedItems { get; }
        public int? TotalCount { get; }
        public int PageIndex { get; }
        public int PageSize { get; }
        public int? PageCount { get; }

        public PaginatedList()
        {
            PaginatedItems = Array.Empty<T>();
        }

        public PaginatedList(T[] paginatedItems, int? totalCount, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            PageSize = pageSize;
            PageCount = totalCount == null ? null : (int) Math.Ceiling(totalCount.Value / (double) pageSize);

            PaginatedItems = paginatedItems;
            TotalCount = totalCount;
        }

        public bool HasNextPage => PaginatedItems.Length >= PageSize;
        public bool HasPrevPage => PageIndex > 0;

        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> query, int pageIndex, int pageSize, int? count = null)
        {
            return await CreateLinqAsync(query, e => e, pageIndex, pageSize, count);
        }

        public static async Task<PaginatedList<T>> CreateLinqAsync<TQuery>(
            IQueryable<TQuery> query,
            Func<IEnumerable<TQuery>, IEnumerable<T>> convert,
            int pageIndex, int pageSize, int? optCount = null, bool showTotal = true)
        {
            var count = showTotal ? optCount ?? await query.CountAsync() : (int?) null;
            var items = await query
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToArrayAsync();

            return new PaginatedList<T>(convert(items).ToArray(), count, pageIndex, pageSize);
        }
    }
}
