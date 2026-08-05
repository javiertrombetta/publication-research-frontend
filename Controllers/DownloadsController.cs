using Microsoft.AspNetCore.Mvc;
using ResearchPublicationManagementSystem.Infrastructure.Api;

namespace ResearchPublicationManagementSystem.Controllers
{
    /// <summary>
    /// Serves the files a reviewer has to read before deciding anything.
    ///
    /// A proxy rather than a link straight to the API: the browser holds an encrypted cookie and
    /// never sees the bearer token, so it cannot fetch a protected file itself. These actions do it
    /// server-side and stream the result back. Who may read what is decided by the API, which
    /// checks access against the publication. Nothing here grants anything.
    /// </summary>
    public class DownloadsController(
        PublicationsApiClient publicationsApi,
        EthicsApiClient ethicsApi) : Controller
    {
        /// <summary>
        /// The current draft of a research paper: for the supervisor reviewing it, the committee
        /// evaluating it, the coordinator deciding on it, and the head of that department.
        ///
        /// The version is resolved here rather than carried in the listing, so a page showing
        /// twenty papers costs nothing extra. Only the one somebody actually opens does.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Paper(Guid publicationId, Guid? versionId, CancellationToken cancellationToken)
        {
            var resolved = versionId;

            if (resolved is null)
            {
                var versions = await publicationsApi.GetVersionsAsync(publicationId, ct: cancellationToken);
                resolved = versions.Data?
                    .OrderByDescending(v => v.VersionNumber)
                    .Select(v => (Guid?)v.Id)
                    .FirstOrDefault();

                if (resolved is null)
                {
                    return NotFound("This research paper has no uploaded version yet.");
                }
            }

            var file = await publicationsApi.DownloadVersionAsync(publicationId, resolved.Value, cancellationToken);

            return file is null
                ? NotFound("That research paper could not be downloaded.")
                : File(file.Value.Content, file.Value.ContentType, file.Value.FileName);
        }

        /// <summary>One ethics document, for the people asked to approve it.</summary>
        [HttpGet]
        public async Task<IActionResult> EthicsDocument(Guid containerId, Guid documentId, CancellationToken cancellationToken)
        {
            var file = await ethicsApi.DownloadDocumentAsync(containerId, documentId, cancellationToken);

            return file is null
                ? NotFound("That document could not be downloaded.")
                : File(file.Value.Content, file.Value.ContentType, file.Value.FileName);
        }
    }
}
