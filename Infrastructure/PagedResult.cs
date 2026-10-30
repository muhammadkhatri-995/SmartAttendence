using Microsoft.EntityFrameworkCore;

namespace SmartAttendance.Web.Infrastructure;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public int FirstItem => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
    public int LastItem => Math.Min(Page * PageSize, TotalCount);

    /// <summary>The query must already be ordered. Out-of-range pages are clamped.</summary>
    public static async Task<PagedResult<T>> CreateAsync(
        IQueryable<T> query, int page, int pageSize, CancellationToken ct)
    {
        var total = await query.CountAsync(ct);
        var pages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, pages);

        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<T>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public PaginationModel ToPagination(Dictionary<string, string> routeValues) => new()
    {
        Page = Page,
        TotalPages = TotalPages,
        TotalCount = TotalCount,
        FirstItem = FirstItem,
        LastItem = LastItem,
        RouteValues = routeValues
    };
}

/// <summary>What the _Pagination partial needs.</summary>
public class PaginationModel
{
    public int Page { get; init; }
    public int TotalPages { get; init; }
    public int TotalCount { get; init; }
    public int FirstItem { get; init; }
    public int LastItem { get; init; }
    public Dictionary<string, string> RouteValues { get; init; } = new();
}