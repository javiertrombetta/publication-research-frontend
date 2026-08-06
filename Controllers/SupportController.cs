using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchPublicationManagementSystem.Infrastructure.Api;
using ResearchPublicationManagementSystem.Models.Messages;

namespace ResearchPublicationManagementSystem.Controllers
{
    /// <summary>
    /// Writing to the institution's IT desk.
    ///
    /// Every role, because everybody with an account can have a problem with the site. Signed in
    /// only, because the desk supports the institution's own people, and a form open to the world
    /// that emails attachments to a fixed address is a relay for whoever finds it. Visitors on the
    /// sign-in page are still offered the address itself, to write to from their own mail client,
    /// which is what the footer has always given them.
    /// </summary>
    [Authorize]
    public class SupportController(SupportApiClient supportApi) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> ContactIt(string? returnUrl = null)
        {
            var model = new ContactItViewModel
            {
                ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null
            };

            var options = await supportApi.GetContactOptionsAsync();
            if (!options.Success)
            {
                TempData["ErrorMessage"] = options.ErrorMessage ?? "Could not reach the server.";
                model.LoadFailed = true;
                return View(model);
            }

            model.ThroughTheSite = options.Data!.ThroughTheSite;
            model.EmailAddress = options.Data.EmailAddress;
            model.MaximumLength = options.Data.MaximumLength;
            model.MaximumAttachments = options.Data.MaximumAttachments;
            model.MaximumAttachmentMegabytes = options.Data.MaximumAttachmentMegabytes;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(40_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 40_000_000)]
        public async Task<IActionResult> ContactIt(string subject, string body, List<IFormFile>? files, string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                TempData["ErrorMessage"] = "Write something before sending it.";
                return RedirectToAction(nameof(ContactIt), new { returnUrl });
            }

            var result = await supportApi.ContactAsync(subject ?? string.Empty, body, files);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Could not send that message.";
                return RedirectToAction(nameof(ContactIt), new { returnUrl });
            }

            TempData["SuccessMessage"] = "Sent to the IT desk. They will reply to your email address.";

            // Back where they were when they pressed Contact IT, if that is somewhere on this site.
            // Checked rather than trusted: a return address in a query string is somewhere anybody
            // can point.
            return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl!) : RedirectToAction(nameof(ContactIt));
        }
    }
}
