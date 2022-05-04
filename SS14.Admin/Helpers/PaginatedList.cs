using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SS14.Admin.Helpers
{
    public interface IPaginatedList
    {
        int PageIndex { get; }
        int PageSize { get; }
        bool HasNextPage { get; }
        bool HasPrevPage { get; }
    }

    public sealed class PaginatedList<T> : IPaginatedList
    {
        public T[] PaginatedItems { get; }
        public int PageIndex { get; }
        public int PageSize { get; }

        public PaginatedList()
        {
            PaginatedItems = Array.Empty<T>();
        }

        public PaginatedList(T[] paginatedItems, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            PageSize = pageSize;

            PaginatedItems = paginatedItems;
        }

        public bool HasNextPage => PaginatedItems.Length >= PageSize;
        public bool HasPrevPage => PageIndex > 0;

        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> query, int pageIndex, int pageSize)
        {
            return await CreateLinqAsync(query, e => e, pageIndex, pageSize);
        }

        public static async Task<PaginatedList<T>> CreateLinqAsync<TQuery>(
            IQueryable<TQuery> query,
            Func<IEnumerable<TQuery>, IEnumerable<T>> convert,
            int pageIndex, int pageSize)
        {
            var items = await query
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToArrayAsync();

            return new PaginatedList<T>(convert(items).ToArray(), pageIndex, pageSize);
        }
    }
}
