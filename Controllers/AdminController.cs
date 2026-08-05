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
        public async Task<IActionResult> Dashboard(
            int page = 1, string? sort = null, bool desc = false, string? search = null)
        {
            var model = new AdminDashboardViewModel
            {
                // Newest activity first when nobody has asked for anything, and exactly what was
                // asked for once somebody has. Defaulting the parameter itself to true was the
                // bug: a heading asking for ascending order says nothing about direction at all,
                // so every heading came back descending and ascending could not be reached.
                Sort = sort ?? "activity",
                Descending = sort is null || desc,
                Search = search
            };

            var summary = await adminApi.GetSummaryAsync();
            if (!summary.Success || summary.Data is null)
            {
                TempData["ErrorMessage"] = summary.ErrorMessage ?? "Could not load the summary right now.";
                model.LoadFailed = true;
                return View(model);
            }

            model.Summary = summary.Data;

            // The figure alone: this card is a link to the queue, not the queue.
            var awaiting = await FindPapersAwaitingCommitteeAsync(pageSize: 1);
            model.PapersAwaitingCommittee = awaiting.Page?.TotalCount ?? 0;

            // And the institution's publications themselves, a page at a time. The cards above
            // count them by one property or another; this is the only place an administrator can
            // look one up without knowing beforehand which screen it would be sitting on. Paged,
            // searched and ordered by the API, so it holds up against a whole institution rather
            // than against a demonstration set.
            var containers = await containersApi.GetAllAsync(
                page: page, sort: model.Sort, descending: model.Descending, search: search);

            if (containers.Success)
            {
                model.Publications = containers.Data?.Items ?? [];
                model.PublicationsTotal = containers.Data?.TotalCount ?? 0;
                model.Pager = Paging.PagerFor(containers.Data, "Admin", nameof(Dashboard), model.RouteValues());
            }
            else
            {
                // The cards are still worth showing: a summary that loaded is not made wrong by a
                // listing that did not.
                TempData["ErrorMessage"] = containers.ErrorMessage ?? "Could not load the publications.";
            }

            return View(model);
        }

        // ---------- The administrator's step in the workflow ----------

        /// <summary>
        /// Papers a supervisor has accepted that still have no evaluation committee. Nothing moves
        /// until one is assigned, because the coordinator's final decision is blocked on it.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> assigning_committee_members(
            int page = 1, int inProgressPage = 1, string? search = null)
        {
            var model = new AssignCommitteeViewModel { Search = search };

            var (items, waiting) = await FindPapersAwaitingCommitteeAsync(page, search);
            model.Items = items;
            model.WaitingTotal = waiting?.TotalCount ?? 0;

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

            // Two listings on one screen, so each pager turns a key of its own. Sharing one would
            // mean paging the queue also paged the committees already sitting.
            model.WaitingPager = Paging.PagerFor(waiting, "Admin", nameof(assigning_committee_members),
                model.RouteValues());

            model.InProgressPager = Paging.PagerFor(inProgress.Data, "Admin", nameof(assigning_committee_members),
                model.RouteValues(), pageKey: "inProgressPage");

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
        /// The publications behind one figure on the dashboard.
        ///
        /// Every count there was a number and nothing else, so an administrator who saw six papers
        /// waiting on a committee had no way from that six to the six. Each figure now links here
        /// with its own filter, and every row from here opens the publication itself, where
        /// documents go on and off and the step is set, exactly as from Assignments.
        /// </summary>
        /// <param name="heading">
        /// What the figure was called on the dashboard, carried so this screen says what it is
        /// showing in the words the reader clicked. Display only; the filter is what selects.
        /// </param>
        [HttpGet]
        public async Task<IActionResult> publications(
            string? status = null, string? pipeline = null, string? paperStatus = null,
            string? ethicsStatus = null, string? committeeDecision = null, string? reviewDecision = null,
            string? heading = null, int page = 1, string? search = null, string? sort = null, bool desc = false)
        {
            var model = new PublicationsAdminViewModel
            {
                Status = status,
                Pipeline = pipeline,
                PaperStatus = paperStatus,
                EthicsStatus = ethicsStatus,
                CommitteeDecision = committeeDecision,
                ReviewDecision = reviewDecision,
                Heading = string.IsNullOrWhiteSpace(heading) ? "Publications" : heading,
                Search = search,
                Sort = sort,
                Descending = desc,

                // Two of the dashboard's figures count something other than publications, and this
                // listing can only show publications, so the two will not agree. Said here rather
                // than left for somebody to notice: an unexplained mismatch between a figure and
                // the list it opened reads as a bug, and doubt about one number is doubt about all
                // of them.
                Description = !string.IsNullOrWhiteSpace(reviewDecision)
                    ? "That figure counts reviews. A paper read by three people counts three times "
                      + "there and appears once here."
                    : !string.IsNullOrWhiteSpace(committeeDecision)
                      && !string.Equals(committeeDecision, "Any", StringComparison.OrdinalIgnoreCase)
                        ? "That figure counts committee members. A committee of three counts three "
                          + "times there and appears once here."
                        : null
            };

            var result = await containersApi.GetByTallyAsync(
                status, pipeline, paperStatus, ethicsStatus, committeeDecision, reviewDecision,
                page, search, sort, desc);

            if (!result.Success || result.Data is null)
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Could not load those publications right now.";
                model.LoadFailed = true;
                return View(model);
            }

            model.Publications = result.Data.Items;
            model.TotalCount = result.Data.TotalCount;
            model.Pager = Paging.PagerFor(result.Data, "Admin", nameof(publications), model.RouteValues());

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
        public async Task<IActionResult> publication(Guid id, int historyPage = 1, string? tab = null, string? from = null)
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
                ActiveTab = tab ?? "progress",
                CameFrom = from
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
                new Dictionary<string, string?> { ["id"] = id.ToString(), ["tab"] = "history", ["from"] = from },
                "historyPage");

            return View(model);
        }

        // ---------- What is in the public catalogue ----------

        /// <summary>
        /// Which accepted papers are in the public catalogue and which are not, with the controls
        /// to move one either way.
        ///
        /// Publishing is the author's decision, and it stays theirs: this is here for the paper
        /// whose author has left, and for taking one out again, which only an administrator can
        /// do. Both cost a reason.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> catalogue(
            int page = 1, int acceptedPage = 1, string? search = null, string? sort = null, bool desc = false)
        {
            var model = new CatalogueAdminViewModel { Search = search, Sort = sort, Descending = desc };

            var published = await containersApi.GetByPaperStatusAsync("Published", page, search, sort, desc);
            var accepted = await containersApi.GetByPaperStatusAsync("Accepted", acceptedPage, search, sort, desc);

            if (!published.Success || !accepted.Success)
            {
                TempData["ErrorMessage"] =
                    published.ErrorMessage ?? accepted.ErrorMessage ?? "Could not load the catalogue right now.";
                model.LoadFailed = true;
                return View(model);
            }

            model.Published = published.Data?.Items ?? [];
            model.Unpublished = accepted.Data?.Items ?? [];
            model.PublishedTotal = published.Data?.TotalCount ?? 0;
            model.UnpublishedTotal = accepted.Data?.TotalCount ?? 0;

            // Each listing turns its own key, or paging one would page the other with it.
            model.Pager = Paging.PagerFor(published.Data, "Admin", nameof(catalogue), model.RouteValues());
            model.UnpublishedPager = Paging.PagerFor(accepted.Data, "Admin", nameof(catalogue), model.RouteValues(),
                pageKey: "acceptedPage");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WithdrawFromCatalogue(
            Guid publicationId, string? comments, string? search, string? sort, bool desc)
        {
            var result = await publicationsApi.UnpublishAsync(publicationId, new CommentsRequestDto(comments ?? string.Empty));

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? "Withdrawn. The paper stays accepted and is no longer in the public catalogue."
                : result.ErrorMessage ?? "Could not withdraw that paper.";

            return RedirectToAction(nameof(catalogue), new { search, sort, desc });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishOnBehalf(
            Guid publicationId, string? comments, string? search, string? sort, bool desc)
        {
            var result = await publicationsApi.PublishDecisionAsync(publicationId,
                new PublishDecisionRequestDto(true, comments ?? string.Empty));

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? "Published. It is in the public catalogue, and the history records that you did it."
                : result.ErrorMessage ?? "Could not publish that paper.";

            return RedirectToAction(nameof(catalogue), new { search, sort, desc });
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

        // ---------- Correcting which proposal a publication runs on ----------

        /// <summary>
        /// Throws away every proposal on the publication and asks the student for a new set.
        ///
        /// The coordinator has this on their own screen, but only while the publication is still
        /// choosing. Once a proposal is assigned it leaves that screen for good, and a set that
        /// turns out to be wrong afterwards had nobody who could do anything about it.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestNewProposals(Guid id, string? comments)
        {
            if (string.IsNullOrWhiteSpace(comments))
            {
                return Refuse(id, "Say why a new set of proposals is being asked for.");
            }

            var result = await proposalsApi.RequestResubmissionAsync(id, new CommentsRequestDto(comments));

            return Done(id, result.Success, result.ErrorMessage,
                "The student has been asked for a new set of proposals, and told why.");
        }

        /// <summary>
        /// Settles the publication on a different one of its proposals. Who supervises it is
        /// unchanged: that is Assignments, above.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeAssignedProposal(Guid id, Guid proposalId, string? comments)
        {
            if (string.IsNullOrWhiteSpace(comments))
            {
                return Refuse(id, "Say why the publication is changing proposal.");
            }

            var result = await proposalsApi.ChangeAssignedProposalAsync(
                proposalId, new CommentsRequestDto(comments));

            return Done(id, result.Success, result.ErrorMessage,
                "This publication now runs on that proposal. Everyone working on it has been told.");
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
        /// <summary>
        /// One page of the papers with no committee yet, and how many there are altogether. The
        /// dashboard needs only the figure, so it asks for the shortest page there is rather than
        /// fetching a queue it is not going to draw.
        /// </summary>
        private async Task<(List<AwaitingCommitteeItem> Items, PagedResultDto<AwaitingCommitteeDto>? Page)>
            FindPapersAwaitingCommitteeAsync(int page = 1, string? search = null, int pageSize = Paging.AsConfigured)
        {
            var awaiting = await publicationsApi.GetAwaitingCommitteeAsync(page, search, pageSize);

            return ([.. (awaiting.Data?.Items ?? []).Select(paper => new AwaitingCommitteeItem { Paper = paper })],
                awaiting.Data);
        }
    }
}
