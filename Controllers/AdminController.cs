using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchPublicationManagementSystem.Common;
using ResearchPublicationManagementSystem.Infrastructure.Api;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;
using ResearchPublicationManagementSystem.Models;

namespace ResearchPublicationManagementSystem.Controllers
{
    /// <summary>
    /// The administrator's own screens: the institution-wide picture, the one workflow step that
    /// belongs to them — putting an evaluation committee on a submitted paper — and the committee
    /// defaults. User management lives in UsersController and the trail in AuditLogsController.
    /// </summary>
    [Authorize(Roles = RoleNames.Admin)]
    public class AdminController(
        AdminApiClient adminApi,
        PublicationsApiClient publicationsApi,
        CommitteesApiClient committeesApi,
        UsersApiClient usersApi,
        SettingsApiClient settingsApi) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var model = new AdminDashboardViewModel();

            var summary = await adminApi.GetSummaryAsync();
            if (!summary.Success || summary.Data is null)
            {
                TempData["ErrorMessage"] = summary.ErrorMessage ?? "Could not load the summary right now.";
                model.LoadFailed = true;
                return View(model);
            }

            model.Summary = summary.Data;

            var awaiting = await FindPapersAwaitingCommitteeAsync();
            model.PapersAwaitingCommittee = awaiting.Count;

            return View(model);
        }

        // ---------- The administrator's step in the workflow ----------

        /// <summary>
        /// Papers a supervisor has accepted that still have no evaluation committee. Nothing moves
        /// until one is assigned — the coordinator's final decision is blocked on it.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> assigning_committee_members()
        {
            var model = new AssignCommitteeViewModel();

            var items = await FindPapersAwaitingCommitteeAsync();
            model.Items = items;

            // Anyone who works here can sit on a committee, so this is the whole enabled directory
            // less the students — a committee judges a student's work, so it cannot be drawn from
            // the people whose work is being judged. One request rather than one per role, and it
            // no longer hides a supervisor or a coordinator the administrator wanted to appoint.
            var people = await usersApi.GetAllAsync(status: "Enabled");

            if (!people.Success)
            {
                TempData["ErrorMessage"] = people.ErrorMessage ?? "Could not load the people who could be appointed.";
                model.LoadFailed = true;
                return View(model);
            }

            model.Members = [.. (people.Data ?? [])
                .Where(u => !u.Roles.Contains(RoleNames.Student))
                .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)];

            // Only needed as a fallback: publications opened before the figures were recorded
            // per publication have none of their own, and the API judges those by today's rules.
            var currentRules = await settingsApi.GetCommitteesAsync();
            model.CurrentRules = currentRules.Data;

            foreach (var item in model.Items)
            {
                item.RequiredInternal = item.Paper.RequiredInternalCommitteeMembers
                                        ?? currentRules.Data?.InternalMembers ?? 0;
                item.RequiredExternal = item.Paper.RequiredExternalCommitteeMembers
                                        ?? currentRules.Data?.ExternalMembers ?? 0;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignCommittee(
            Guid publicationId, Guid[] memberUserIds, int minApprovalsRequired, string? comments)
        {
            if (memberUserIds.Length == 0)
            {
                TempData["ErrorMessage"] = "Choose at least one committee member.";
                return RedirectToAction(nameof(assigning_committee_members));
            }

            if (minApprovalsRequired < 1 || minApprovalsRequired > memberUserIds.Length)
            {
                TempData["ErrorMessage"] =
                    $"The number of approvals required must be between 1 and {memberUserIds.Length}.";
                return RedirectToAction(nameof(assigning_committee_members));
            }

            var result = await committeesApi.AssignAsync(publicationId,
                new AssignCommitteeRequestDto(memberUserIds, minApprovalsRequired, comments ?? string.Empty));

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? $"Committee assigned. {memberUserIds.Length} " +
                  (memberUserIds.Length == 1 ? "member has" : "members have") + " been asked to evaluate the paper."
                : result.ErrorMessage ?? "Could not assign the committee.";

            return RedirectToAction(nameof(assigning_committee_members));
        }

        // ---------- Settings ----------

        // ---------- Still to be wired ----------

        [HttpGet]
        public IActionResult Admin_check_proposaldetail() => View();

        // ---------- Helpers ----------

        /// <summary>
        /// A paper needs a committee when it is under review and has none. There is no endpoint
        /// that asks that directly, so it is assembled from the container listing (which carries
        /// the paper's status) plus a committee lookup per candidate — a short list in practice,
        /// since only papers between the supervisor's review and the coordinator's decision qualify.
        /// </summary>
        /// <summary>
        /// The API answers this in one request. It used to be reconstructed here by walking every
        /// container and asking after each one's paper and committee — two further requests per
        /// publication — and it still came out wrong: nothing in those responses says whether the
        /// supervisor has approved, so papers they had not yet looked at were offered for a
        /// committee and the assignment was then refused.
        /// </summary>
        private async Task<List<AwaitingCommitteeItem>> FindPapersAwaitingCommitteeAsync()
        {
            var awaiting = await publicationsApi.GetAwaitingCommitteeAsync();

            return [.. (awaiting.Data ?? []).Select(paper => new AwaitingCommitteeItem { Paper = paper })];
        }
    }
}
