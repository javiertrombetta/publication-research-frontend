using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchPublicationManagementSystem.Infrastructure.Api;
using ResearchPublicationManagementSystem.Services;

namespace ResearchPublicationManagementSystem.Controllers
{
    /// <summary>
    /// Profile photo management. Deliberately not on StudentController: every role has a profile
    /// photo, and that controller is Student-only.
    /// </summary>
    [Authorize]
    public class ProfileController(UsersApiClient usersApi, IAuthCookieService authCookieService) : Controller
    {
        private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];

        /// <summary>
        /// Landing page for a signed-in user with no operational role yet (the Staff role the
        /// document describes: they can sign in and see their profile, but nothing else until
        /// an Admin grants them a role).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Me()
        {
            var result = await usersApi.GetMeAsync();
            return View(result.Data);
        }

        /// <summary>
        /// Streams a user's photo to the browser. The API needs a bearer token that only lives
        /// server-side in the auth cookie, so the image cannot be linked directly and is proxied
        /// through here instead.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Photo(Guid? id)
        {
            var userId = id ?? CurrentUserId();
            if (userId is null) return NotFound();

            var photo = await usersApi.GetProfilePhotoAsync(userId.Value);
            if (photo is null) return NotFound();

            return File(photo.Value.Content, photo.Value.ContentType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(10_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 10_000_000)]
        public async Task<IActionResult> UploadPhoto(IFormFile? photo, string? returnUrl = null)
        {
            if (photo is not { Length: > 0 })
            {
                TempData["ErrorMessage"] = "Choose an image to upload.";
                return RedirectBack(returnUrl);
            }

            // Cheap client-side-ish guard for a clearer message; the backend enforces the real
            // rule by file extension and size.
            if (!AllowedContentTypes.Contains(photo.ContentType))
            {
                TempData["ErrorMessage"] = "Profile photos must be a JPG, PNG or WebP image.";
                return RedirectBack(returnUrl);
            }

            var result = await usersApi.UploadProfilePhotoAsync(photo);
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Could not update your profile photo.";
                return RedirectBack(returnUrl);
            }

            await authCookieService.SetProfilePhotoFlagAsync(HttpContext, true);
            TempData["SuccessMessage"] = "Profile photo updated.";
            return RedirectBack(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePhoto(string? returnUrl = null)
        {
            var result = await usersApi.DeleteProfilePhotoAsync();
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Could not remove your profile photo.";
                return RedirectBack(returnUrl);
            }

            await authCookieService.SetProfilePhotoFlagAsync(HttpContext, false);
            TempData["SuccessMessage"] = "Profile photo removed.";
            return RedirectBack(returnUrl);
        }

        private Guid? CurrentUserId() =>
            Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

        private IActionResult RedirectBack(string? returnUrl) =>
            !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? Redirect(returnUrl)
                : RedirectToAction("studentprofile", "Student");
    }
}
