using Microsoft.EntityFrameworkCore;

namespace MotelLease.Application.Common.Contracts;

/// <summary>The paging envelope every list endpoint returns (docs/api-design.md, Conventions).</summary>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int Total,
    int TotalPages);

public static class Paged
{
    /// <summary>
    /// An upper bound on the page size. Without one, <c>?pageSize=100000</c> is a way to ask
    /// the database for the whole table.
    /// </summary>
    public const int MaxPageSize = 100;

    public const int DefaultPageSize = 20;

    public static async Task<PagedResponse<T>> FromAsync<T>(
        IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<T>(
            items,
            page,
            pageSize,
            total,
            (int)Math.Ceiling(total / (double)pageSize));
    }
}
