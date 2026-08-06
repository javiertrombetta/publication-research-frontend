using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchPublicationManagementSystem.Common;
using ResearchPublicationManagementSystem.Infrastructure.Api;
using ResearchPublicationManagementSystem.Models.Messages;

namespace ResearchPublicationManagementSystem.Controllers
{
    /// <summary>
    /// Writing to the people working on a publication, and reading what they wrote back.
    ///
    /// One screen for every role rather than one per role. Who a person may write to differs, and
    /// the API decides that; what the screen does with the answer is the same for all of them, and
    /// six copies of it is six places for the wording to drift apart.
    /// </summary>
    [Authorize]
    public class MessagesController(
        ContainerMessagesApiClient messagesApi, ContainersApiClient containersApi) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Publication(Guid id, Guid? with = null, int page = 1)
        {
            var model = new PublicationMessagesViewModel
            {
                ContainerId = id,
                With = with,
                Page = page,
                BackUrl = BackTo(id)
            };

            var context = await messagesApi.GetContextAsync(id);
            if (!context.Success)
            {
                TempData["ErrorMessage"] = context.ErrorMessage ?? "Could not open the messages on this publication.";
                model.LoadFailed = true;
                return View(model);
            }

            model.Enabled = context.Data!.Enabled;
            model.Counterparts = context.Data.Counterparts;
            model.AllowedExtensions = context.Data.AllowedExtensions;
            model.MaximumLength = context.Data.MaximumLength;
            model.MaximumAttachments = context.Data.MaximumAttachments;

            // Opened on whoever is waiting, or failing that on the conversation last had. The API
            // already returns them in that order, so this is its first entry with any history at
            // all. Somebody with three names and no history sees the chooser, which is the only
            // honest answer there.
            //
            // A single name opens whether or not anything has been said: there is nothing to
            // choose between, so a chooser would be a page asking a question with one answer.
            if (with is null)
            {
                model.With = model.Counterparts.FirstOrDefault(c => c.UnreadFromThem > 0 || c.LastMessageAt is not null)?.UserId
                             ?? (model.Counterparts.Count == 1 ? model.Counterparts[0].UserId : null);
            }

            var listing = await messagesApi.GetMessagesAsync(id, model.With, page);
            if (listing.Success)
            {
                model.Messages = listing.Data?.Items ?? [];
                model.Page = listing.Data?.Page ?? 1;
                model.TotalPages = listing.Data?.TotalPages ?? 1;
                model.TotalCount = listing.Data?.TotalCount ?? 0;
            }

            // Reading is what opening it means. Done after the listing is fetched so the messages
            // being marked are the ones on screen, and only on the first page: paging back through
            // an old conversation is not reading the new one.
            if (model.With is { } other && page == 1)
            {
                var marked = await messagesApi.MarkReadAsync(id, other);

                // The counterparts were counted before that, so the person whose conversation is
                // open still carried a badge saying how many were waiting, on the screen that had
                // just shown them. Cleared here rather than by asking the API again: the answer is
                // known, and a second round trip to learn it is a second round trip.
                if (marked.Success)
                {
                    model.Counterparts = [.. model.Counterparts.Select(c =>
                        c.UserId == other ? c with { UnreadFromThem = 0 } : c)];
                }
            }

            var container = await containersApi.GetByIdAsync(id);
            if (container.Success)
            {
                model.PublicationTitle = container.Data?.Title;
                model.StudentName = container.Data?.StudentName;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 100_000_000)]
        public async Task<IActionResult> Send(Guid id, Guid recipientUserId, string body, List<IFormFile>? files)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                TempData["ErrorMessage"] = "Write something before sending it.";
                return RedirectToAction(nameof(Publication), new { id, with = recipientUserId });
            }

            var result = await messagesApi.SendAsync(id, recipientUserId, body, files);

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? "Sent."
                : result.ErrorMessage ?? "Could not send that message.";

            return RedirectToAction(nameof(Publication), new { id, with = recipientUserId });
        }

        [HttpGet]
        public async Task<IActionResult> Attachment(Guid id, Guid attachmentId)
        {
            var file = await messagesApi.DownloadAttachmentAsync(id, attachmentId);
            if (file is null)
            {
                TempData["ErrorMessage"] = "Could not open that file.";
                return RedirectToAction(nameof(Publication), new { id });
            }

            return File(file.Value.Content, file.Value.ContentType, file.Value.FileName);
        }

        /// <summary>
        /// Where the reader came from. Every role reaches a publication through its own screen, and
        /// a Back that goes to somebody else's is worse than none.
        /// </summary>
        private string? BackTo(Guid containerId)
        {
            if (User.IsInRole(RoleNames.Student))
            {
                return Url.Action("Publication", "Student", new { id = containerId });
            }

            if (User.IsInRole(RoleNames.Admin))
            {
                return Url.Action("publication", "Admin", new { id = containerId });
            }

            if (User.IsInRole(RoleNames.Coordinator))
            {
                return Url.Action("Publication", "Coordinator", new { id = containerId });
            }

            if (User.IsInRole(RoleNames.HeadOfDepartment))
            {
                return Url.Action("Publication", "HeadOfDepartment", new { id = containerId });
            }

            if (User.IsInRole(RoleNames.Supervisor))
            {
                return Url.Action("SupervisorDashboard", "Supervisor");
            }

            return null;
        }
    }
}
