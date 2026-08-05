using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchPublicationManagementSystem.Common;
using ResearchPublicationManagementSystem.Infrastructure.Api;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;
using ResearchPublicationManagementSystem.Models;
using ResearchPublicationManagementSystem.Services;

namespace ResearchPublicationManagementSystem.Controllers
{
    /// <summary>
    /// The signed-in person's own account: their profile, their photo, their password. Deliberately
    /// not on StudentController. Every role has these, and that controller is Student-only.
    /// </summary>
    [Authorize]
    public class ProfileController(
        UsersApiClient usersApi,
        AuthApiClient authApi,
        IAuthCookieService authCookieService) : Controller
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
        /// The person correcting their own details.
        ///
        /// Everybody signs themselves up, so everybody can mistype their own name, programme or
        /// cohort, and until now nobody could put it right: the screen showed the details and
        /// offered no way to change them, so a typo needed an administrator. The API has taken
        /// this since the beginning and nothing called it.
        ///
        /// What it does not take is deliberate. The address is what they sign in with, the
        /// department is the institution's to say, and the roles are the administrator's; each of
        /// those is somebody else's decision about them rather than their own account of
        /// themselves. The API refuses them too, so this is not the only thing holding the line.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveProfile(
            string firstName, string lastName, string? programme, string? cohort, string? orcid,
            string? areasOfExpertise, string? researchInterests)
        {
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                TempData["ErrorMessage"] = "Your name cannot be left blank.";
                return RedirectToAction(nameof(Me));
            }

            // The API reads null as "leave this as it was", so what decides whether a field is sent
            // is whether the form showed it, not whether it came back empty. Judged by role, the
            // same way the form decides what to draw: a supervisor's request must not carry a
            // programme, and a student who clears their ORCID must be able to clear it rather than
            // find it silently kept.
            var student = User.IsInRole(RoleNames.Student);
            var supervisor = User.IsInRole(RoleNames.Supervisor);

            var result = await usersApi.UpdateMeAsync(new UpdateMyProfileRequestDto(
                firstName.Trim(), lastName.Trim(),
                student ? (programme ?? string.Empty).Trim() : null,
                student ? (cohort ?? string.Empty).Trim() : null,
                null,
                student ? (orcid ?? string.Empty).Trim() : null,
                null,
                supervisor ? (areasOfExpertise ?? string.Empty).Trim() : null,
                supervisor ? (researchInterests ?? string.Empty).Trim() : null));

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? "Your details are saved."
                : result.ErrorMessage ?? "Could not save your details.";

            return RedirectToAction(nameof(Me));
        }

        /// <summary>
        /// The person saying whether they are taking work on.
        ///
        /// Theirs alone, and separate from the administrator enabling or disabling the account:
        /// this governs what they are offered next, and leaves anything already assigned to them
        /// exactly where it is. Nothing reads a student's, since no decision in the system chooses
        /// a student, so the control is only shown to the roles it means something for.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetAvailability(bool isAvailable)
        {
            var result = await usersApi.SetMyAvailabilityAsync(isAvailable);

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? (isAvailable
                    ? "You are available for new work again."
                    : "You will not be offered new work. Anything already assigned to you is unaffected.")
                : result.ErrorMessage ?? "Could not change your availability.";

            return RedirectToAction(nameof(Me));
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

        // ---------- Changing your own password ----------

        [HttpGet]
        public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

        /// <summary>
        /// The confirmation is checked here and the current password by the API. Keeping them
        /// apart matters: only the API can tell whether the current password is right, and only
        /// it counts a wrong one towards locking the account.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.NewPassword != model.ConfirmPassword)
            {
                ModelState.AddModelError(nameof(model.ConfirmPassword),
                    "The two new passwords do not match.");
                return View(model);
            }

            if (model.NewPassword == model.CurrentPassword)
            {
                ModelState.AddModelError(nameof(model.NewPassword),
                    "Your new password must be different from your current one.");
                return View(model);
            }

            var accessToken = User.FindFirst(AuthClaimTypes.AccessToken)?.Value;
            if (accessToken is null)
            {
                return RedirectToAction("home", "Auth");
            }

            var result = await authApi.ChangePasswordAsync(
                new ChangePasswordRequestDto(model.CurrentPassword, model.NewPassword), accessToken);

            if (!result.Success)
            {
                // Covers a wrong current password, a new one the rules reject, and the lockout that
                // follows too many wrong attempts, each with the API's own wording.
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Could not change your password.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Your password has been changed.";
            return RedirectToAction(nameof(Me));
        }

    }
}
