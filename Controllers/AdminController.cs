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
        ContainersApiClient containersApi,
        PublicationsApiClient publicationsApi,
        CommitteesApiClient committeesApi,
        UsersApiClient usersApi,
        SettingsApiClient settingsApi,
        DepartmentsApiClient departmentsApi,
        ProposalsApiClient proposalsApi,
        EthicsApiClient ethicsApi,
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
            var supervisors = await usersApi.GetAllAsync(role: RoleNames.Supervisor, pageSize: 100);
            model.Supervisors = [.. (supervisors.Data?.Items ?? [])
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
        public async Task<IActionResult> assigning_committee_members(int inProgressPage = 1)
        {
            var model = new AssignCommitteeViewModel();

            var items = await FindPapersAwaitingCommitteeAsync();
            model.Items = items;

            // Asked for as a list of candidates rather than assembled here from the whole directory.
            // Who may be appointed is a rule with several parts, and an administrator now chooses
            // some of them, so working it out a second time on this side would eventually offer
            // somebody the save then refuses.
            var people = await committeesApi.GetCandidatesAsync();

            if (!people.Success)
            {
                TempData["ErrorMessage"] = people.ErrorMessage ?? "Could not load the people who could be appointed.";
                model.LoadFailed = true;
                return View(model);
            }

            model.Members = people.Data ?? [];

            // Only needed as a fallback: publications opened before the figures were recorded
            // per publication have none of their own, and the API judges those by today's rules.
            var currentRules = await settingsApi.GetCommitteesAsync();
            model.CurrentRules = currentRules.Data;

            foreach (var item in model.Items)
            {
                item.RequiredReviewers = item.Paper.RequiredReviewerMembers
                                        ?? currentRules.Data?.ReviewerMembers ?? 0;
                item.RequiredExternal = item.Paper.RequiredExternalCommitteeMembers
                                        ?? currentRules.Data?.ExternalMembers ?? 0;
            }

            // Committees already sitting, so one can be changed after the fact. A failure here is
            // not worth failing the screen for: appointing new ones still works, and the section
            // says when it is empty.
            var inProgress = await committeesApi.GetInProgressAsync(page: inProgressPage);
            model.InProgress = inProgress.Data?.Items ?? [];
            model.InProgressTotal = inProgress.Data?.TotalCount ?? 0;
            model.InProgressPager = Paging.PagerFor(inProgress.Data, "Admin", nameof(assigning_committee_members),
                pageKey: "inProgressPage");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignCommittee(
            Guid publicationId, Guid[] memberUserIds, int minApprovalsRequired, string? comments,
            bool overrideComposition = false)
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

            // Departing from the composition this publication was opened under is a decision
            // somebody owns, so it does not go through on a blank reason. Caught here as well as
            // at the API, since the answer is the same and the person is still on the screen.
            if (overrideComposition && string.IsNullOrWhiteSpace(comments))
            {
                TempData["ErrorMessage"] =
                    "Say why this publication is being given a committee of a different shape. It stays on the publication's history.";
                return RedirectToAction(nameof(assigning_committee_members));
            }

            var result = await committeesApi.AssignAsync(publicationId,
                new AssignCommitteeRequestDto(memberUserIds, minApprovalsRequired, comments ?? string.Empty,
                    overrideComposition));

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? $"Committee assigned. {memberUserIds.Length} " +
                  (memberUserIds.Length == 1 ? "member has" : "members have") + " been asked to evaluate the paper."
                  + (overrideComposition ? " The composition you chose, and your reason, are on the publication's history." : string.Empty)
                : result.ErrorMessage ?? "Could not assign the committee.";

            return RedirectToAction(nameof(assigning_committee_members));
        }

        /// <summary>
        /// Changes a committee that is already sitting: who is on it, and how many approvals it
        /// needs. Always with a reason, and refused by the API once the committee has finished.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCommittee(
            Guid committeeId, Guid[] memberUserIds, int minApprovalsRequired, string? comments,
            bool overrideComposition = false)
        {
            if (memberUserIds.Length == 0)
            {
                TempData["ErrorMessage"] = "A committee needs at least one member. To end one, the coordinator decides on the paper.";
                return RedirectToAction(nameof(assigning_committee_members));
            }

            if (string.IsNullOrWhiteSpace(comments))
            {
                TempData["ErrorMessage"] =
                    "Say why this committee is being changed. It stays on the publication's history.";
                return RedirectToAction(nameof(assigning_committee_members));
            }

            var result = await committeesApi.UpdateAsync(committeeId,
                new UpdateCommitteeRequestDto(memberUserIds, minApprovalsRequired, comments, overrideComposition));

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? $"Committee updated. It now has {memberUserIds.Length} "
                  + (memberUserIds.Length == 1 ? "member" : "members") + "."
                : result.ErrorMessage ?? "Could not change the committee.";

            return RedirectToAction(nameof(assigning_committee_members));
        }

        // ---------- Who is responsible for what ----------

        /// <summary>
        /// Publications still under way and the people carrying them. Every step of the pipeline
        /// waits on somebody named on the publication, so one who leaves or falls ill stops it,
        /// and until now nothing could name anybody else.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> assignments(int page = 1, string? search = null)
        {
            var model = new AssignmentsViewModel { Search = search };

            var containers = await containersApi.GetAllAsync(status: "InProgress", page: page, search: search);
            if (!containers.Success)
            {
                TempData["ErrorMessage"] = containers.ErrorMessage ?? "Could not load the publications right now.";
                model.LoadFailed = true;
                return View(model);
            }

            model.Publications = containers.Data?.Items ?? [];
            model.TotalCount = containers.Data?.TotalCount ?? 0;
            model.Pager = Paging.PagerFor(containers.Data, "Admin", nameof(assignments), model.RouteValues());

            // Everyone holding the role, not only those free this week: this screen fixes a
            // publication that is stuck, and leaving somebody out because they marked themselves
            // busy would hide the very person the work is being moved to.
            var supervisors = await usersApi.GetAllAsync(role: RoleNames.Supervisor, pageSize: 100);
            model.Supervisors = [.. (supervisors.Data?.Items ?? []).OrderBy(u => u.LastName).ThenBy(u => u.FirstName)];

            // Coordinators and heads of department come per department instead, because both posts
            // are held in one and only somebody in the student's own may take the work on. One
            // request per department on the page, not per publication.
            foreach (var departmentId in model.Publications
                         .Select(p => p.StudentDepartmentId)
                         .OfType<Guid>()
                         .Distinct())
            {
                var members = await departmentsApi.GetMembersAsync(departmentId);
                if (members.Data is not null)
                {
                    model.ByDepartment[departmentId] = members.Data;
                }
            }

            return View(model);
        }

        /// <summary>
        /// What a publication actually holds, read only: its proposals, its ethics decision and
        /// documents, its paper and versions, and everything that has happened to it.
        ///
        /// An administrator could move people around a publication without ever seeing what was in
        /// it, which is deciding in the dark: whether a supervisor should be replaced depends on
        /// what is sitting unread on their desk. Nothing here can be changed from this screen.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> publication(Guid id, int historyPage = 1, string? tab = null)
        {
            var container = await containersApi.GetByIdAsync(id);
            if (!container.Success || container.Data is null)
            {
                TempData["ErrorMessage"] = container.ErrorMessage ?? "Could not open that publication.";
                return RedirectToAction(nameof(assignments));
            }

            var model = new PublicationDetailViewModel
            {
                Container = container.Data,
                ActiveTab = tab ?? "progress"
            };

            var proposals = await proposalsApi.GetByContainerAsync(id);
            model.Proposals = proposals.Data ?? [];

            if (container.Data.CurrentPipeline >= PipelineStage.EthicsApproval)
            {
                var ethics = await ethicsApi.GetApprovalAsync(id);
                if (ethics.Success) model.EthicsApproval = ethics.Data;

                var documents = await ethicsApi.GetDocumentsAsync(id);
                model.EthicsDocuments = documents.Data ?? [];
            }

            if (container.Data.CurrentPipeline >= PipelineStage.ResearchPaper)
            {
                var paper = await publicationsApi.GetByContainerAsync(id);
                if (paper.Success) model.Publication = paper.Data;

                if (model.Publication is { } written)
                {
                    var versions = await publicationsApi.GetVersionsAsync(written.Id);
                    model.PaperVersions = versions.Data ?? [];
                }
            }

            // Best-effort, as on every other screen that shows a trail: a publication is still
            // worth reading when its history cannot be.
            // What can be added, for the administrator's own upload. Best-effort: the rest of the
            // screen reads perfectly well without the means to add a document.
            var requirements = await settingsApi.GetEthicsDocumentsAsync();
            model.EthicsRequirements = [.. (requirements.Data ?? []).Where(r => r.IsActive)];

            var history = await containersApi.GetActivityHistoryAsync(id, historyPage);
            model.History = history.Data?.Items ?? [];
            model.HistoryTotal = history.Data?.TotalCount ?? 0;
            model.HistoryPager = Paging.PagerFor(history.Data, "Admin", nameof(publication),
                new Dictionary<string, string?> { ["id"] = id.ToString(), ["tab"] = "history" }, "historyPage");

            return View(model);
        }

        // ---------- Correcting what a publication holds, and where it stands ----------

        /// <summary>
        /// Puts a document on a running publication, or takes one off, and does the same for the
        /// paper's versions. Every one of them costs a reason.
        ///
        /// None of them moves the publication. Where it should stand afterwards is the separate
        /// decision below, so that a file put right and the person who picks it up next are not
        /// tangled together.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(200_000_000)]
        public async Task<IActionResult> AddEthicsDocument(Guid id, string documentType, IFormFile? file, string? comments)
        {
            if (file is null || file.Length == 0)
            {
                return Refuse(id, "Choose a file to add.");
            }

            var result = await ethicsApi.AdminUploadDocumentAsync(id, documentType, file, comments ?? string.Empty);
            return Done(id, result.Success, result.ErrorMessage,
                "Document added, unread. Check where the publication now stands below.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveEthicsDocument(Guid id, Guid documentId, string? comments)
        {
            var result = await ethicsApi.AdminRemoveDocumentAsync(id, documentId, comments ?? string.Empty);
            return Done(id, result.Success, result.ErrorMessage,
                "Document removed. Check where the publication now stands below.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(200_000_000)]
        public async Task<IActionResult> AddPaperVersion(Guid id, Guid publicationId, IFormFile? file, string? comments)
        {
            if (file is null || file.Length == 0)
            {
                return Refuse(id, "Choose a file to add.");
            }

            var result = await publicationsApi.AdminUploadVersionAsync(publicationId, file, comments ?? string.Empty);
            return Done(id, result.Success, result.ErrorMessage,
                "Version added. The paper's status is unchanged; set it below if it should move.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePaperVersion(Guid id, Guid publicationId, Guid versionId, string? comments)
        {
            var result = await publicationsApi.AdminRemoveVersionAsync(publicationId, versionId, comments ?? string.Empty);
            return Done(id, result.Success, result.ErrorMessage,
                "Version removed. The paper's status is unchanged; set it below if it should move.");
        }

        /// <summary>
        /// Sets which step of which stage the publication waits at, which is what actually lets
        /// people carry on: a document put right is no use while the stage still says the person
        /// who needed it has had their turn.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MovePublication(
            Guid id, int stage, string? ethicsStep, string? paperStatus, string? comments)
        {
            var result = await containersApi.MoveAsync(id,
                new MoveContainerRequestDto(stage, comments ?? string.Empty, ethicsStep, paperStatus));

            return Done(id, result.Success, result.ErrorMessage,
                "Moved. Whoever it now waits on has been told.");
        }

        private IActionResult Refuse(Guid id, string why)
        {
            TempData["ErrorMessage"] = why;
            return RedirectToAction(nameof(publication), new { id });
        }

        private IActionResult Done(Guid id, bool success, string? error, string message)
        {
            TempData[success ? "SuccessMessage" : "ErrorMessage"] = success ? message : error ?? "That did not work.";
            return RedirectToAction(nameof(publication), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reassign(
            Guid id, Guid? coordinatorUserId, Guid? supervisorUserId, Guid? headOfDepartmentUserId,
            string? comments, string? search)
        {
            if (string.IsNullOrWhiteSpace(comments))
            {
                TempData["ErrorMessage"] =
                    "Say why these assignments are being changed. It stays on the publication's history.";
                return RedirectToAction(nameof(assignments), new { search });
            }

            var result = await containersApi.ReassignAsync(id,
                new ReassignContainerRequestDto(coordinatorUserId, supervisorUserId, comments, headOfDepartmentUserId));

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? "Assignments changed. Whoever now has the work has been told."
                : result.ErrorMessage ?? "Could not change the assignments.";

            return RedirectToAction(nameof(assignments), new { search });
        }

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
