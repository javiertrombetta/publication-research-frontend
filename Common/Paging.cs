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
    /// Chosen to fill a screen without filling a scroll. Each of these rows is a card or a table
    /// block rather than a line, so ten is already a long page.
    /// </summary>
    public const int DefaultPageSize = 10;

    public static int TotalPages(int itemCount, int pageSize = DefaultPageSize) =>
        itemCount <= 0 ? 1 : (int)Math.Ceiling(itemCount / (double)pageSize);

    /// <summary>
    /// Clamps rather than rejects: a page number from an old link, or typed into the address bar,
    /// should show the nearest real page instead of an error or an empty list.
    /// </summary>
    public static int ClampPage(int page, int itemCount, int pageSize = DefaultPageSize) =>
        Math.Clamp(page, 1, TotalPages(itemCount, pageSize));

    public static List<T> Page<T>(IReadOnlyList<T> all, int page, int pageSize = DefaultPageSize) =>
        [.. all.Skip((ClampPage(page, all.Count, pageSize) - 1) * pageSize).Take(pageSize)];
}
