using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchPublicationManagementSystem.Common;
using ResearchPublicationManagementSystem.Infrastructure.Api;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;
using ResearchPublicationManagementSystem.Models;

namespace ResearchPublicationManagementSystem.Controllers
{
    /// <summary>
    /// Invitations: how someone gets an account when self-registration is closed, and the only
    /// route that ever existed for external committee members — they have no institutional
    /// address, so no email domain could say what they are.
    ///
    /// Sending and withdrawing are an administrator's. Accepting is deliberately anonymous: the
    /// person doing it has no account yet, which is the point.
    /// </summary>
    [Authorize(Roles = RoleNames.Admin)]
    public class InvitationsController(
        InvitationsApiClient invitationsApi,
        DepartmentsApiClient departmentsApi) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new InvitationsViewModel();

            var invitations = await invitationsApi.GetAllAsync();
            if (!invitations.Success)
            {
                TempData["ErrorMessage"] = invitations.ErrorMessage ?? "Could not load the invitations.";
                model.LoadFailed = true;
                return View(model);
            }

            model.Invitations = invitations.Data ?? [];

            var departments = await departmentsApi.GetAllAsync();
            model.Departments = departments.Data ?? [];

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(
            string email, string role, string firstName, string lastName, Guid? departmentId)
        {
            var result = await invitationsApi.CreateAsync(
                new CreateInvitationRequestDto(email, role, firstName, lastName, departmentId));

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? $"Invitation sent to {email}."
                : result.ErrorMessage ?? "Could not send the invitation.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resend(Guid id)
        {
            var result = await invitationsApi.ResendAsync(id);

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? "Sent again. The previous link no longer works."
                : result.ErrorMessage ?? "Could not send the invitation again.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Revoke(Guid id)
        {
            var result = await invitationsApi.RevokeAsync(id);

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? "Invitation withdrawn. The link no longer works."
                : result.ErrorMessage ?? "Could not withdraw the invitation.";

            return RedirectToAction(nameof(Index));
        }

        // ---------- What the invited person sees ----------

        /// <summary>
        /// Anonymous by necessity — this person has no account yet. The token in the link is the
        /// only credential, and it is unguessable, single-use and time-limited.
        /// </summary>
        [HttpGet("/accept-invitation")]
        [AllowAnonymous]
        public async Task<IActionResult> Accept(string? token)
        {
            var model = new AcceptInvitationViewModel { Token = token ?? string.Empty };

            var preview = await invitationsApi.PreviewAsync(model.Token);
            if (!preview.Success)
            {
                model.Problem = preview.ErrorMessage ?? "This invitation link is not valid.";
                return View(model);
            }

            model.Invitation = preview.Data;
            return View(model);
        }

        [HttpPost("/accept-invitation")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(AcceptInvitationViewModel model)
        {
            // Re-read every time rather than trusted from the form: between the page loading and
            // being submitted the invitation may have been withdrawn or have expired.
            var preview = await invitationsApi.PreviewAsync(model.Token);
            if (!preview.Success)
            {
                model.Problem = preview.ErrorMessage ?? "This invitation link is not valid.";
                return View(model);
            }

            model.Invitation = preview.Data;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError(nameof(model.ConfirmPassword), "The two passwords do not match.");
                return View(model);
            }

            var result = await invitationsApi.AcceptAsync(
                new AcceptInvitationRequestDto(model.Token, model.Password));

            if (!result.Success)
            {
                // Covers a password the rules reject and an invitation that has just been
                // withdrawn, each in the API's own words.
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Could not set up your account.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Your account is ready. Sign in with the password you just chose.";
            return RedirectToAction("home", "Auth");
        }
    }
}
