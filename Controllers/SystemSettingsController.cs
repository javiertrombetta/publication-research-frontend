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
        UsersApiClient usersApi,
        DepartmentsApiClient departmentsApi,
        Services.IInstitutionDetails institution) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index(string? tab, Guid? departmentId)
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
            var storage = await settingsApi.GetStorageAsync();

            // One failure fails the screen: showing three groups and a blank fourth would invite
            // someone to "correct" values that are only blank because they did not load.
            if (!committees.Success || !passwords.Success || !notifications.Success || !ethicsDocuments.Success
                || !access.Success || !uploads.Success || !institution.Success || !deadlines.Success
                || !storage.Success)
            {
                TempData["ErrorMessage"] =
                    committees.ErrorMessage ?? passwords.ErrorMessage ?? notifications.ErrorMessage
                    ?? ethicsDocuments.ErrorMessage ?? access.ErrorMessage ?? uploads.ErrorMessage
                    ?? institution.ErrorMessage ?? deadlines.ErrorMessage ?? storage.ErrorMessage
                    ?? "Could not load the system settings.";
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
            model.Storage = storage.Data!;

            // Who an administrator can leave out of committee work. Only worth fetching on the tab
            // that shows it, and a failure here is not worth failing the whole screen for: the rest
            // of the settings are readable without it, and the list says so when it is empty.
            if (model.ActiveTab == "committees")
            {
                var people = await usersApi.GetAllAsync(pageSize: 100);
                model.CommitteePeople = [.. (people.Data?.Items ?? [])
                    .Where(u => u.Roles.Any(model.Committees.SelectableRoles.Contains))
                    .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)];
            }

            // The departments, and whoever is in the one being looked at. Falls back to the first
            // department so the tab opens on something rather than on a prompt to choose.
            if (model.ActiveTab == "departments")
            {
                var departments = await departmentsApi.GetAllAsync();
                model.Departments = departments.Data ?? [];

                var chosen = departmentId ?? model.Departments.FirstOrDefault()?.Id;
                if (chosen is not null)
                {
                    var members = await departmentsApi.GetMembersAsync(chosen.Value);
                    model.DepartmentMembers = members.Data;
                    if (!members.Success)
                    {
                        TempData["ErrorMessage"] = members.ErrorMessage ?? "Could not load who is in that department.";
                    }
                }

                var heads = await usersApi.GetAllAsync(role: RoleNames.HeadOfDepartment, pageSize: 200);
                var coordinators = await usersApi.GetAllAsync(role: RoleNames.Coordinator, pageSize: 200);
                model.HeadCandidates = Sorted(heads.Data?.Items);
                model.CoordinatorCandidates = Sorted(coordinators.Data?.Items);
            }

            // The API's answer, not this application's guess. It used to ask its own environment,
            // on the assumption that the two run together; deployed as separate services that
            // assumption broke, and the hosted testing deployment greyed out a choice the API
            // would have accepted. Hiding a choice the API would refuse is still right, but only
            // the API knows which those are.
            model.CanOpenRegistration = access.Data!.CanOpenRegistration;

            return View(model);
        }

        // ---------- Committees ----------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCommittees(
            int reviewerMembers, int externalMembers, int minimumApprovals,
            string[]? candidateRoles = null, Guid[]? excludedUserIds = null)
        {
            // Sent as empty arrays rather than left out when the form posted none, so clearing every
            // exclusion is something an administrator can actually do. A null would mean "leave this
            // alone", which is what a caller that does not manage the setting wants.
            var result = await settingsApi.UpdateCommitteesAsync(
                new UpdateCommitteeSettingsRequestDto(
                    reviewerMembers, externalMembers, minimumApprovals,
                    candidateRoles ?? [], excludedUserIds ?? []));

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

        // ---------- Where uploaded files are kept ----------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveStorage(
            string provider, string? localPath, string? s3Bucket, string? s3Region, string? s3ServiceUrl,
            string? s3AccessKeyId, string? s3SecretKey, bool s3ForcePathStyle,
            string? azureContainer, string? azureConnectionString, bool copyExisting = false)
        {
            // Blank means "leave the stored secret alone". An administrator cannot read these back,
            // so an empty box has to mean unchanged rather than cleared.
            var result = await settingsApi.UpdateStorageAsync(new UpdateStorageSettingsRequestDto(
                provider, localPath, s3Bucket, s3Region, s3ServiceUrl, s3AccessKeyId,
                string.IsNullOrWhiteSpace(s3SecretKey) ? null : s3SecretKey,
                s3ForcePathStyle, azureContainer,
                string.IsNullOrWhiteSpace(azureConnectionString) ? null : azureConnectionString));

            if (!result.Success)
            {
                return Done(false, "storage", result.ErrorMessage, string.Empty);
            }

            // Only if asked. The default is what it has always been: the destination changes and
            // the files already stored stay where they are, which costs nothing and breaks nothing.
            if (copyExisting)
            {
                var moved = await settingsApi.MigrateStorageAsync();

                TempData[moved.Success ? "SuccessMessage" : "ErrorMessage"] = moved.Success
                    ? Describe(moved.Data)
                    : moved.ErrorMessage ?? "Saved, but the existing files could not be copied.";

                return RedirectToAction(nameof(Index), new { tab = "storage" });
            }

            return Done(true, "storage", null,
                "Saved. New uploads go to the new destination; files already stored keep opening from where they are.");
        }

        /// <summary>
        /// Copies whatever is still elsewhere. Its own button as well as a tick on the save form,
        /// because a run is bounded and a large collection needs asking more than once.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CopyExistingFiles()
        {
            var result = await settingsApi.MigrateStorageAsync();

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? Describe(result.Data)
                : result.ErrorMessage ?? "Could not copy the existing files.";

            return RedirectToAction(nameof(Index), new { tab = "storage" });
        }

        /// <summary>
        /// What the run did, in a sentence. The problems are named rather than counted: an
        /// administrator who is told three files failed and not which ones cannot do anything.
        /// </summary>
        private static string Describe(StorageMigrationResultDto? result)
        {
            if (result is null) return "Nothing to copy.";

            var said = result.Moved == 1 ? "One file copied." : $"{result.Moved} files copied.";

            if (result.Remaining > 0)
            {
                said += $" {result.Remaining} still to go: run it again to continue.";
            }
            else if (result.Problems.Count == 0)
            {
                said += " Everything is now at the destination in force.";
            }

            if (result.Problems.Count > 0)
            {
                said += " Could not copy: " + string.Join("; ", result.Problems.Take(5));
                if (result.Problems.Count > 5) said += $" and {result.Problems.Count - 5} more.";
            }

            return said + " The originals were left where they were.";
        }

        /// <summary>
        /// Tries the destination and reports back on the same screen, so an administrator finds out
        /// that a bucket name is wrong here rather than when a student uploads.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckStorage(string? provider)
        {
            var result = await settingsApi.CheckStorageAsync(provider);

            TempData[result.Data is { Reachable: true } ? "SuccessMessage" : "ErrorMessage"] =
                result.Data?.Message ?? result.ErrorMessage ?? "Could not test the destination.";

            return RedirectToAction(nameof(Index), new { tab = "storage" });
        }

        // ---------- The institution ----------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveInstitution(
            string name, string studentEmailDomain, string staffEmailDomain,
            string? itSupportEmail, string? researchEnquiriesEmail, string? privacyPolicyUrl,
            string? currentAcademicCycle, string? websiteUrl, int rowsPerPage,
            bool itSupportShownToVisitors = false)
        {
            var result = await settingsApi.UpdateInstitutionAsync(new UpdateInstitutionSettingsRequestDto(
                name, studentEmailDomain, staffEmailDomain, itSupportEmail, researchEnquiriesEmail,
                privacyPolicyUrl, currentAcademicCycle, websiteUrl, rowsPerPage,
                itSupportShownToVisitors));

            // The footer reads this, and it is cached per request for a minute. Without dropping
            // that, an administrator would tick the box and watch the page below them disagree.
            institution.Invalidate();

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

        // ---------- Departments ----------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDepartment(string name, string code)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(code))
            {
                return DepartmentDone(false, null, "Give the department a name and a code.", null);
            }

            var result = await departmentsApi.CreateAsync(
                new CreateDepartmentRequestDto(name.Trim(), code.Trim().ToUpperInvariant()));

            return DepartmentDone(result.Success, result.Data?.Id, result.ErrorMessage,
                "Added. Give it a head of department and its coordinators before students are sent to it.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RenameDepartment(Guid id, string name, string code)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(code))
            {
                return DepartmentDone(false, id, "A department needs a name and a code.", null);
            }

            var result = await departmentsApi.UpdateAsync(id,
                new UpdateDepartmentRequestDto(name.Trim(), code.Trim().ToUpperInvariant()));

            return DepartmentDone(result.Success, id, result.ErrorMessage,
                "Saved. The new name shows everywhere the department is named.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveDepartment(Guid id)
        {
            var result = await departmentsApi.RemoveAsync(id);

            return DepartmentDone(result.Success, result.Success ? null : id, result.ErrorMessage,
                "Removed.");
        }

        /// <summary>
        /// The department's heads and coordinators, as a whole list. Naming somebody moves them here
        /// from wherever they were, so this is where a department is arranged; the API refuses to
        /// leave anybody holding one of those roles in no department at all.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDepartmentMembers(
            Guid id, Guid[]? headOfDepartmentUserIds = null, Guid[]? coordinatorUserIds = null)
        {
            var result = await departmentsApi.SetMembersAsync(id,
                new SetDepartmentMembersRequestDto(headOfDepartmentUserIds ?? [], coordinatorUserIds ?? []));

            return DepartmentDone(result.Success, id, result.ErrorMessage,
                "Saved. Anybody moved here keeps the role they already had.");
        }

        // ---------- Helpers ----------

        /// <summary>Back to the departments tab, still looking at the department worked on.</summary>
        private IActionResult DepartmentDone(bool success, Guid? departmentId, string? error, string? successMessage)
        {
            TempData[success ? "SuccessMessage" : "ErrorMessage"] =
                success ? successMessage ?? "Saved." : error ?? "Could not save the department.";

            return RedirectToAction(nameof(Index), new { tab = "departments", departmentId });
        }

        /// <summary>People in the order a list of names is read in.</summary>
        private static IReadOnlyList<UserListItemDto> Sorted(IReadOnlyList<UserListItemDto>? people) =>
            [.. (people ?? []).OrderBy(u => u.LastName).ThenBy(u => u.FirstName)];

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
                or "institution" or "deadlines" or "storage" or "departments" => tab,
            _ => "committees"
        };
    }
}
