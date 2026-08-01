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
    /// belongs to them, putting an evaluation committee on a submitted paper, and the committee
    /// defaults. User management lives in UsersController and the trail in AuditLogsController.
    /// </summary>
    [Authorize(Roles = RoleNames.Admin)]
    public class AdminController(
        AdminApiClient adminApi,
        PublicationsApiClient publicationsApi,
        CommitteesApiClient committeesApi,
        UsersApiClient usersApi,
        SettingsApiClient settingsApi,
        SupervisorGroupsApiClient groupsApi) : Controller
    {
        // ---------- Coordinators' saved supervisor groups ----------

        /// <summary>
        /// Every coordinator's groups, with the controls to rename one, change who is in it, or
        /// throw it away. Somebody has to be able to clear out lists left behind by people who
        /// have moved on, and the coordinator who owns one is not always still here to do it.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> supervisor_groups(string? search = null)
        {
            var model = new SupervisorGroupsViewModel { Search = search };

            var groups = await groupsApi.GetAllAsync(search);
            if (!groups.Success)
            {
                TempData["ErrorMessage"] = groups.ErrorMessage ?? "Could not load the groups right now.";
                model.LoadFailed = true;
                return View(model);
            }

            model.Groups = groups.Data ?? [];

            // Every supervisor account, not only the available ones. This screen edits lists kept
            // over months, and leaving somebody out because they are away this week would quietly
            // drop them from any group saved while they were.
            var supervisors = await usersApi.GetAllAsync(role: RoleNames.Supervisor);
            model.Supervisors = [.. (supervisors.Data ?? [])
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)];

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSupervisorGroup(
            Guid groupId, string? name, Guid[] supervisorIds, string? search)
        {
            if (string.IsNullOrWhiteSpace(name) || supervisorIds.Length == 0)
            {
                TempData["ErrorMessage"] = "A group needs a name and at least one supervisor.";
                return RedirectToAction(nameof(supervisor_groups), new { search });
            }

            var result = await groupsApi.UpdateAnyAsync(groupId, name.Trim(), supervisorIds);

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? "Group saved."
                : result.ErrorMessage ?? "Could not save the group.";

            return RedirectToAction(nameof(supervisor_groups), new { search });
        }

        /// <summary>
        /// Discards the groups ticked, or every group in the institution when the request says so.
        /// Two ways in rather than one, because "none ticked" must not be able to mean "all".
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSupervisorGroups(Guid[] groupIds, bool all, string? search)
        {
            if (!all && groupIds.Length == 0)
            {
                TempData["ErrorMessage"] = "Tick the groups to delete, or use Delete all.";
                return RedirectToAction(nameof(supervisor_groups), new { search });
            }

            var result = await groupsApi.DeleteManyAsync(groupIds, all);

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? result.Data == 1 ? "One group deleted." : $"{result.Data} groups deleted."
                : result.ErrorMessage ?? "Could not delete the groups.";

            return RedirectToAction(nameof(supervisor_groups), new { search = all ? null : search });
        }

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
        /// until one is assigned, because the coordinator's final decision is blocked on it.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> assigning_committee_members()
        {
            var model = new AssignCommitteeViewModel();

            var items = await FindPapersAwaitingCommitteeAsync();
            model.Items = items;

            // Anyone who works here can sit on a committee, so this is the whole enabled directory
            // less the students. A committee judges a student's work, so it cannot be drawn from
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
        /// the paper's status) plus a committee lookup per candidate, a short list in practice,
        /// since only papers between the supervisor's review and the coordinator's decision
        /// qualify.
        /// </summary>
        /// <summary>
        /// The API answers this in one request. It used to be reconstructed here by walking every
        /// container and asking after each one's paper and committee, two further requests per
        /// publication, and it still came out wrong: nothing in those responses says whether the
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
