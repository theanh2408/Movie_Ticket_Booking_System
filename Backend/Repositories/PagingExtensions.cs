using System.Linq.Expressions;
using Backend.Common;
using Backend.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories;

/// <summary>Phân trang + sắp xếp dùng chung cho mọi API danh sách (BUG-17).</summary>
internal static class PagingExtensions
{
    public static async Task<(List<T> Items, int Total)> ToPagedAsync<T>(this IQueryable<T> query, PageQuery paging, CancellationToken ct = default)
    {
        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public static IOrderedQueryable<T> By<T, TKey>(this IQueryable<T> query, Expression<Func<T, TKey>> key, bool descending) =>
        descending ? query.OrderByDescending(key) : query.OrderBy(key);

    public static BadRequestException UnknownSort(string allowed) =>
        new($"sortBy không hợp lệ. Chỉ nhận: {allowed}");
}
