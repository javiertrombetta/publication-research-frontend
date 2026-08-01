using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchPublicationManagementSystem.Common;
using ResearchPublicationManagementSystem.Infrastructure.Api;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;
using ResearchPublicationManagementSystem.Models;

namespace ResearchPublicationManagementSystem.Controllers
{
    /// <summary>
    /// The rules the whole system runs by: committee composition, the documents the ethics stage
    /// asks for, what counts as an acceptable password, and where notifications are sent.
    ///
    /// Each group saves on its own and returns to its own tab. The alternative, one Save for the
    /// whole page, would mean a rejected mail server silently discarding an unrelated edit.
    /// </summary>
    [Authorize(Roles = RoleNames.Admin)]
    public class SystemSettingsController(
        SettingsApiClient settingsApi,
        IHostEnvironment environment,
        Services.IInstitutionDetails institution) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index(string? tab)
        {
            var model = new SystemSettingsViewModel { ActiveTab = NormaliseTab(tab) };

            var committees = await settingsApi.GetCommitteesAsync();
            var passwords = await settingsApi.GetPasswordsAsync();
            var notifications = await settingsApi.GetNotificationsAsync();
            var ethicsDocuments = await settingsApi.GetEthicsDocumentsAsync();
            var access = await settingsApi.GetAccessAsync();
            var uploads = await settingsApi.GetUploadsAsync();
            var institution = await settingsApi.GetInstitutionAsync();
            var deadlines = await settingsApi.GetDeadlinesAsync();

            // One failure fails the screen: showing three groups and a blank fourth would invite
            // someone to "correct" values that are only blank because they did not load.
            if (!committees.Success || !passwords.Success || !notifications.Success || !ethicsDocuments.Success
                || !access.Success || !uploads.Success || !institution.Success || !deadlines.Success)
            {
                TempData["ErrorMessage"] =
                    committees.ErrorMessage ?? passwords.ErrorMessage ?? notifications.ErrorMessage
                    ?? ethicsDocuments.ErrorMessage ?? access.ErrorMessage ?? uploads.ErrorMessage
                    ?? institution.ErrorMessage ?? deadlines.ErrorMessage ?? "Could not load the system settings.";
                model.LoadFailed = true;
                return View(model);
            }

            model.Committees = committees.Data!;
            model.Passwords = passwords.Data!;
            model.Notifications = notifications.Data!;
            model.EthicsDocuments = ethicsDocuments.Data ?? [];
            model.Access = access.Data!;
            model.Uploads = uploads.Data!;
            model.Institution = institution.Data!;
            model.Deadlines = deadlines.Data!;

            // Asked of this process rather than of the API: the frontend and the API run in the
            // same environment, and hiding a choice the API would refuse is better than offering
            // it and reporting the refusal.
            model.CanOpenRegistration = environment.IsDevelopment();

            return View(model);
        }

        // ---------- Committees ----------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCommittees(int internalMembers, int externalMembers, int minimumApprovals)
        {
            var result = await settingsApi.UpdateCommitteesAsync(
                new UpdateCommitteeSettingsRequestDto(internalMembers, externalMembers, minimumApprovals));

            return Done(result.Success, "committees", result.ErrorMessage,
                "Saved. Publications opened from now on will use these figures; those already under way keep theirs.");
        }

        // ---------- Ethics documents ----------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEthicsDocument(string name, string? description, int sortOrder)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["ErrorMessage"] = "Give the document a name.";
                return RedirectToAction(nameof(Index), new { tab = "ethics" });
            }

            var result = await settingsApi.CreateEthicsDocumentAsync(
                new SaveEthicsDocumentRequirementRequestDto(name, description, sortOrder));

            return Done(result.Success, "ethics", result.ErrorMessage,
                "Added. It will be asked of publications whose ethics stage starts from now on.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEthicsDocument(Guid id, string name, string? description, int sortOrder)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["ErrorMessage"] = "Give the document a name.";
                return RedirectToAction(nameof(Index), new { tab = "ethics" });
            }

            var result = await settingsApi.UpdateEthicsDocumentAsync(
                id, new SaveEthicsDocumentRequirementRequestDto(name, description, sortOrder));

            return Done(result.Success, "ethics", result.ErrorMessage, "Saved.");
        }

        /// <summary>
        /// Retires a document or brings it back. There is no delete on purpose: one that has been
        /// asked of anyone is referenced by what they uploaded.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetEthicsDocumentActive(Guid id, bool isActive)
        {
            var result = await settingsApi.SetEthicsDocumentActiveAsync(id, isActive);

            return Done(result.Success, "ethics", result.ErrorMessage,
                isActive
                    ? "This document will be asked for again."
                    : "This document will no longer be asked for. Publications already asked for it still owe it.");
        }

        // ---------- Passwords ----------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePasswords(
            int minimumLength,
            bool requireDigit,
            bool requireUppercase,
            bool requireLowercase,
            bool requireSymbol,
            int expiryDays,
            int lockoutAttempts,
            int lockoutMinutes)
        {
            var result = await settingsApi.UpdatePasswordsAsync(new UpdatePasswordSettingsRequestDto(
                minimumLength, requireDigit, requireUppercase, requireLowercase, requireSymbol,
                expiryDays, lockoutAttempts, lockoutMinutes));

            return Done(result.Success, "passwords", result.ErrorMessage,
                "Saved. The new rules apply the next time anyone sets a password.");
        }

        // ---------- Notifications ----------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveNotifications(
            bool emailEnabled,
            string? smtpHost,
            int smtpPort,
            string? smtpUsername,
            string? smtpPassword,
            bool useSsl,
            string? fromAddress,
            string? fromName)
        {
            // Blank means "leave the stored password alone". It cannot be read back, so treating
            // an untouched field as a request to clear it would break the mail server every time
            // someone changed the port.
            var password = string.IsNullOrEmpty(smtpPassword) ? null : smtpPassword;

            var result = await settingsApi.UpdateNotificationsAsync(new UpdateNotificationSettingsRequestDto(
                emailEnabled, smtpHost, smtpPort, smtpUsername, password, useSsl, fromAddress, fromName));

            return Done(result.Success, "notifications", result.ErrorMessage,
                emailEnabled
                    ? "Saved. Notifications will be emailed as well as shown in the application."
                    : "Saved. Notifications will appear in the application only.");
        }

        // ---------- Access ----------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAccess(
            string registrationMode, bool azureSsoEnabled, int invitationValidDays,
            int accessTokenMinutes, int refreshTokenDays, bool publicCatalogueEnabled)
        {
            var result = await settingsApi.UpdateAccessAsync(new UpdateAccessSettingsRequestDto(
                registrationMode, azureSsoEnabled, invitationValidDays, accessTokenMinutes,
                refreshTokenDays, publicCatalogueEnabled));

            // The landing page is cached for a minute so it is not fetched on every render; drop
            // that now, or an administrator switching the catalogue off would keep being sent to
            // it and reasonably conclude the setting had not saved.
            if (result.Success) institution.Invalidate();

            return Done(result.Success, "access", result.ErrorMessage,
                publicCatalogueEnabled
                    ? "Saved. The public catalogue is the site's landing page."
                    : "Saved. The public catalogue is off; visitors are shown the sign-in page.");
        }

        // ---------- Uploads ----------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveUploads(int maxMegabytes, string allowedExtensions)
        {
            var result = await settingsApi.UpdateUploadsAsync(
                new UpdateUploadSettingsRequestDto(maxMegabytes, allowedExtensions));

            return Done(result.Success, "uploads", result.ErrorMessage,
                "Saved. Applies to the next file anyone uploads; files already stored are untouched.");
        }

        // ---------- The institution ----------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveInstitution(
            string name, string studentEmailDomain, string staffEmailDomain,
            string? itSupportEmail, string? researchEnquiriesEmail, string? privacyPolicyUrl,
            string? currentAcademicCycle)
        {
            var result = await settingsApi.UpdateInstitutionAsync(new UpdateInstitutionSettingsRequestDto(
                name, studentEmailDomain, staffEmailDomain, itSupportEmail, researchEnquiriesEmail,
                privacyPolicyUrl, currentAcademicCycle));

            return Done(result.Success, "institution", result.ErrorMessage, "Saved.");
        }

        // ---------- Deadlines ----------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDeadlines(
            int supervisorResponseDays, int ethicsReviewDays, int committeeReviewDays)
        {
            var result = await settingsApi.UpdateDeadlinesAsync(new UpdateDeadlineSettingsRequestDto(
                supervisorResponseDays, ethicsReviewDays, committeeReviewDays));

            return Done(result.Success, "deadlines", result.ErrorMessage,
                "Saved. Deadlines mark work as overdue; they never stop it being done late.");
        }

        // ---------- Helpers ----------

        private IActionResult Done(bool success, string tab, string? error, string successMessage)
        {
            TempData[success ? "SuccessMessage" : "ErrorMessage"] =
                success ? successMessage : error ?? "Could not save the setting.";

            return RedirectToAction(nameof(Index), new { tab });
        }

        /// <summary>
        /// Only the tabs that exist, so a hand-edited query string cannot leave the page with
        /// nothing selected.
        /// </summary>
        private static string NormaliseTab(string? tab) => tab switch
        {
            "ethics" or "passwords" or "notifications" or "access" or "uploads"
                or "institution" or "deadlines" => tab,
            _ => "committees"
        };
    }
}
