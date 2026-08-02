namespace ResearchPublicationManagementSystem.Common;

/// <summary>
/// One page of a role's work queue.
///
/// These screens used to render every row the API returned. That is fine for a handful and awful
/// for a department: a head of department with two hundred proposals waited for all of them to be
/// laid out before seeing the first, and then scrolled past the lot to reach anything. What people
/// act on is the first few rows, so that is what a page is.
/// </summary>
public static class Paging
{
    /// <summary>
    /// Ask for no particular length, and take the one the institution has set.
    ///
    /// The normal case. How long a page is was a constant here, which meant an administrator could
    /// not change it and every screen quietly disagreed with the settings page. The API reads the
    /// figure and applies it to any request that names none, so the site's job is to name none.
    /// </summary>
    public const int AsConfigured = 0;

    /// <summary>
    /// What a page is when nobody has said otherwise: the same last resort the API falls back to,
    /// used here for the few listings paged in the browser rather than by the API.
    /// </summary>
    public const int DefaultRowsPerPage = 10;

    public static int TotalPages(int itemCount, int pageSize = DefaultRowsPerPage) =>
        itemCount <= 0 ? 1 : (int)Math.Ceiling(itemCount / (double)pageSize);

    /// <summary>
    /// Clamps rather than rejects: a page number from an old link, or typed into the address bar,
    /// should show the nearest real page instead of an error or an empty list.
    /// </summary>
    public static int ClampPage(int page, int itemCount, int pageSize = DefaultRowsPerPage) =>
        Math.Clamp(page, 1, TotalPages(itemCount, pageSize));

    public static List<T> Page<T>(IReadOnlyList<T> all, int page, int pageSize = DefaultRowsPerPage) =>
        [.. all.Skip((ClampPage(page, all.Count, pageSize) - 1) * pageSize).Take(pageSize)];

    /// <summary>
    /// The page-size parameter for a query string, and nothing at all when no size was asked for.
    /// Sending none is what lets the API apply the institution's own figure.
    /// </summary>
    public static string SizeParam(int pageSize) =>
        pageSize > 0 ? $"&pageSize={pageSize}" : string.Empty;

    /// <summary>The same answer for a parameter dictionary, where absent means a null value.</summary>
    public static string? SizeValue(int pageSize) =>
        pageSize > 0 ? pageSize.ToString() : null;

    /// <summary>Builds the pager from what the API said about the page it returned.</summary>
    public static Models.PagerViewModel PagerFor<T>(
        Infrastructure.Api.Dto.PagedResultDto<T>? result, string controller, string action,
        Dictionary<string, string?>? routeValues = null) => new()
        {
            Controller = controller,
            Action = action,
            Page = result?.Page ?? 1,
            TotalPages = result?.TotalPages ?? 1,
            RouteValues = routeValues ?? []
        };
}
