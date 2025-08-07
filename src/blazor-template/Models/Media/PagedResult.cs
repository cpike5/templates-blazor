namespace BlazorTemplate.Models.Media;

/// <summary>
/// Represents a paginated result set with metadata.
/// </summary>
/// <typeparam name="T">The type of items in the result set.</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// The items for the current page.
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Total number of items across all pages.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number (1-based).
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Indicates if there is a previous page available.
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Indicates if there is a next page available.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Creates an empty paged result.
    /// </summary>
    /// <param name="page">Current page number.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <returns>Empty paged result.</returns>
    public static PagedResult<T> Empty(int page = 1, int pageSize = 20)
    {
        return new PagedResult<T>
        {
            Items = new List<T>(),
            TotalCount = 0,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Creates a paged result from a full list of items.
    /// </summary>
    /// <param name="allItems">All items to paginate.</param>
    /// <param name="page">Current page number.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <returns>Paged result containing the specified page of items.</returns>
    public static PagedResult<T> FromItems(IEnumerable<T> allItems, int page, int pageSize)
    {
        var itemsList = allItems.ToList();
        var pagedItems = itemsList
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<T>
        {
            Items = pagedItems,
            TotalCount = itemsList.Count,
            Page = page,
            PageSize = pageSize
        };
    }
}