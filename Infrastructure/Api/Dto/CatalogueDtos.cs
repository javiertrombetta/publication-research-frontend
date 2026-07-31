namespace ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

/// <summary>
/// A published research paper as the public sees it. Only papers whose author chose to publish
/// them reach the catalogue, so everything here is deliberately public.
/// </summary>
public record CatalogueEntryDto(
    Guid Id,
    string Title,
    string Abstract,
    string AuthorName,
    string? SupervisorName,
    IReadOnlyList<string> Keywords,
    string? PublicationType,
    int? PublicationYear,
    string? Department,
    IReadOnlyList<string> ResearchAreas);

/// <summary>Matches the backend's PagedResult&lt;T&gt;.</summary>
public record PagedResultDto<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;

    /// <summary>1-based index of the first item on this page, for "showing X to Y of Z".</summary>
    public int FirstItemNumber => TotalCount == 0 ? 0 : ((Page - 1) * PageSize) + 1;

    public int LastItemNumber => Math.Min(Page * PageSize, TotalCount);
}

public record CitationDto(string Apa, string Mla);
