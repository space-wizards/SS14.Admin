using System.Linq.Expressions;

namespace SS14.Admin.Helpers;

public static class QueryHelper
{
    public static IQueryable<TOutput> LeftJoin<TLeft, TRight, TKey, TOutput>(
        this IQueryable<TLeft> left,
        IEnumerable<TRight> right,
        Expression<Func<TLeft, TKey>> leftKey,
        Expression<Func<TRight, TKey>> rightKey,
        Expression<Func<TLeft, TRight?, TOutput>> join)
    {
        var paramJ = Expression.Parameter(typeof(LeftJoinInternal<TLeft, TRight>));
        var paramR = Expression.Parameter(typeof(TRight));
        var body = Expression.Invoke(join, Expression.Field(paramJ, "L"), paramR);
        var l = Expression.Lambda<Func<LeftJoinInternal<TLeft, TRight>, TRight, TOutput>>(body, paramJ, paramR);

        return left
            .GroupJoin(right, leftKey, rightKey, (l, r) => new LeftJoinInternal<TLeft, TRight> { L = l, R = r })
            .SelectMany(j => j.R.DefaultIfEmpty()!, l);
    }

    private sealed class LeftJoinInternal<TLeft, TRight>
    {
        public TLeft L = default!;
        public IEnumerable<TRight> R = default!;
    }
}
