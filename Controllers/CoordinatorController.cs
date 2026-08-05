using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchPublicationManagementSystem.Common;
using ResearchPublicationManagementSystem.Infrastructure.Api;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;
using ResearchPublicationManagementSystem.Models;

namespace ResearchPublicationManagementSystem.Controllers
{
    /// <summary>
    /// The coordinator sits between the student and everyone else: they send proposals out to
    /// supervisors, assign the one a supervisor accepts, confirm ethics decisions, and take the
    /// final decision on a research paper.
    /// </summary>
    [Authorize(Roles = RoleNames.Coordinator)]
    public class CoordinatorController(
        ContainersApiClient containersApi,
        ProposalsApiClient proposalsApi,
        EthicsApiClient ethicsApi,
        PublicationsApiClient publicationsApi,
        UsersApiClient usersApi,
        SettingsApiClient settingsApi,
        SupervisorGroupsApiClient groupsApi) : Controller
    {
        // ---------- Overview ----------

        [HttpGet]
        public async Task<IActionResult> Coordinator_dashboard(
            int page = 1, string? sort = null, bool desc = false, string? search = null)
        {
            var model = new CoordinatorDashboardViewModel
            {
                Search = search,
                // The default, spelled out rather than left null, so the heading in force is the
                // one marked as active. Oldest first, as on every other queue.
                Sort = sort ?? "started",
                Descending = desc
            };

            // Scoped to this coordinator, and to work that is still moving. A publication that has
            // been completed is a record rather than a task, and the question this screen answers
            // is what is left to do; leaving them in pushed the live ones onto later pages.
            var containers = await containersApi.GetAllAsync(
                coordinatorId: CurrentUserId(), status: "InProgress", page: page,
                sort: sort ?? "started", descending: desc, search: search);
            if (!containers.Success)
            {
                TempData["ErrorMessage"] = containers.ErrorMessage ?? "Could not load your publications right now.";
                model.LoadFailed = true;
                return View(model);
            }

            model.Publications = containers.Data?.Items ?? [];
            model.PublicationsTotal = containers.Data?.TotalCount ?? 0;
            model.Pager = Paging.PagerFor(containers.Data, "Coordinator", nameof(Coordinator_dashboard),
                model.RouteValues());

            var pending = await proposalsApi.GetPendingAsync();
            model.ProposalsAwaitingDispatch = pending.Data?.Items ?? [];
            model.ProposalsAwaitingDispatchTotal = pending.Data?.TotalCount ?? 0;

            // The same queue Supervisor selections works from, asked only for its size. The card
            // for it used to show a dash, which said the figure was unavailable when it was one
            // request away, and a dashboard that states two figures and withholds a third reads as
            // broken rather than as reserved.
            var awaitingAllocation = await proposalsApi.GetForCoordinatorAsync(page: 1, awaitingAllocation: true);
            model.SupervisorRepliesTotal = awaitingAllocation.Data?.TotalCount ?? 0;

            // The two ethics queues, by size. The card here used to count publications, which is a
            // figure that only grows and says nothing about what is waiting; a coordinator reading
            // the top of this screen wants to know what is theirs to do.
            var ethicsFirst = await containersApi.GetAllAsync(
                coordinatorId: CurrentUserId(), ethicsSteps: EthicsSteps.CoordinatorFirstReview,
                page: 1, pageSize: 1);
            model.EthicsDecisionsTotal = ethicsFirst.Data?.TotalCount ?? 0;

            var ethicsFinal = await containersApi.GetAllAsync(
                coordinatorId: CurrentUserId(), ethicsSteps: EthicsSteps.CoordinatorFinalDecision,
                page: 1, pageSize: 1);
            model.FinalEthicsDecisionsTotal = ethicsFinal.Data?.TotalCount ?? 0;

            return View(model);
        }

        // ---------- Pipeline 1: sending proposals out and assigning a supervisor ----------

        /// <summary>
        /// Submitted proposals waiting to go to supervisors, and the supervisors they can be sent
        /// to. Proposals go out as a batch, because a supervisor is choosing between them.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> assigning_proposal_forsupervisor(
            int page = 1, string? supervisorSearch = null,
            string? sort = null, bool desc = false, string? search = null) =>
            View(await LoadDispatchScreenAsync(page, supervisorSearch, sort, desc, search));

        /// <summary>
        /// The whole dispatch screen, built the same way whether it is being opened or redrawn
        /// after a send was refused. Refusing has to put back the queue in the order the
        /// coordinator was reading it, so the listing cannot be rebuilt from the endpoint's
        /// defaults on the way back.
        /// </summary>
        private async Task<AssignProposalsViewModel> LoadDispatchScreenAsync(
            int page, string? supervisorSearch, string? sort, bool desc, string? search)
        {
            var model = new AssignProposalsViewModel
            {
                Page = page < 1 ? 1 : page,
                SupervisorSearch = supervisorSearch,
                // The default, not null: the bar has to show which column the list is ordered by,
                // and "none of them" would be a lie about a list that is ordered.
                Sort = sort ?? "submitted",
                Descending = desc,
                Search = search
            };

            // Oldest first unless asked otherwise. A dispatch queue is worked from the front, and
            // the student who has been waiting longest should be nearest the top rather than
            // buried under everything submitted since.
            var pending = await proposalsApi.GetPendingAsync(
                page, sort: sort ?? "submitted", descending: desc, search: search);
            if (!pending.Success)
            {
                TempData["ErrorMessage"] = pending.ErrorMessage ?? "Could not load the proposals waiting to be sent.";
                model.LoadFailed = true;
                return model;
            }

            model.Proposals = pending.Data?.Items ?? [];

            // The API narrows this to supervisors who are enabled and who have not marked
            // themselves unavailable, so what arrives is already the people who can be asked.
            var supervisors = await usersApi.GetSupervisorsAsync(search: supervisorSearch);
            var available = (supervisors.Data?.Items ?? [])
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToList();

            // All of them, paged in the browser rather than here.
            //
            // A page turn that reloads the screen would lose every supervisor already ticked, and
            // "select all" would only ever mean the ten on screen. The browser already holds the
            // list, so turning a page is a matter of which rows it shows, and a tick made on page
            // one is still a tick when the coordinator is looking at page three. Without
            // JavaScript nothing is hidden and the list is simply long, which is correct, just
            // less comfortable.
            model.Supervisors = available;
            model.SupervisorsTotal = available.Count;

            // The coordinator's own saved sets. Failing to load them is not failing to load the
            // screen: the chooser still works one name at a time, which is what it did before
            // groups existed.
            var groups = await groupsApi.GetMineAsync();
            model.Groups = groups.Data ?? [];

            // Counted over the whole queue rather than this page, because it is the figure that
            // decides what to do next: send a second batch, or ask those students for new work.
            var returned = await proposalsApi.GetReturnedToDispatchAsync();
            model.ReturnedStudents = returned.Data?.Students ?? 0;
            model.ReturnedProposals = returned.Data?.Proposals ?? 0;

            // The same call carries the date to start the answer-by field on. Shown in the
            // reader's own time, because that is the only one they think in.
            if (returned.Data is { } dispatch)
            {
                model.SuggestedRespondBy = dispatch.SuggestedRespondBy.ToLocalTime();
            }

            model.TotalCount = pending.Data?.TotalCount ?? 0;
            model.Pager = Paging.PagerFor(pending.Data, "Coordinator", nameof(assigning_proposal_forsupervisor),
                model.RouteValues());

            return model;
        }

        /// <summary>
        /// Saves whoever is ticked at the moment as a named group, so the same set can be picked
        /// by name next time instead of rebuilt by hand.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSupervisorGroup(string? name, Guid[] groupSupervisorIds)
        {
            if (string.IsNullOrWhiteSpace(name) || groupSupervisorIds.Length == 0)
            {
                TempData["ErrorMessage"] = "Give the group a name and tick at least one supervisor first.";
                return RedirectToAction(nameof(assigning_proposal_forsupervisor));
            }

            var result = await groupsApi.CreateAsync(name.Trim(), groupSupervisorIds);

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? $"Saved “{name.Trim()}” as a group of {groupSupervisorIds.Length}."
                : result.ErrorMessage ?? "Could not save the group.";

            return RedirectToAction(nameof(assigning_proposal_forsupervisor));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSupervisorGroup(Guid groupId)
        {
            var result = await groupsApi.DeleteAsync(groupId);

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? "Group deleted."
                : result.ErrorMessage ?? "Could not delete the group.";

            return RedirectToAction(nameof(assigning_proposal_forsupervisor));
        }

        /// <summary>
        /// Asks the student for a fresh set of proposals, because the ones they wrote found nobody
        /// willing to supervise them. The other way out of that is to send the same proposals to
        /// different supervisors, which is the ordinary send.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AskForNewProposals(Guid containerId, string? comments)
        {
            if (string.IsNullOrWhiteSpace(comments))
            {
                TempData["ErrorMessage"] =
                    "Say why you are asking for new proposals. The student sees this and has nothing else to go on.";
                return RedirectToAction(nameof(assigning_proposal_forsupervisor));
            }

            var result = await proposalsApi.RequestResubmissionAsync(
                containerId, new CommentsRequestDto(comments.Trim()));

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? "Asked for a new set of proposals. The student has been notified."
                : result.ErrorMessage ?? "Could not ask for new proposals.";

            return RedirectToAction(nameof(assigning_proposal_forsupervisor));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendToSupervisors(
            Guid[] proposalIds, Guid[] supervisorIds, string? comments, DateTime? respondBy,
            int page = 1, string? supervisorSearch = null,
            string? sort = null, bool desc = false, string? search = null)
        {
            // Every refusal comes back to the screen as the coordinator left it: the same page of
            // the same queue in the same order, with the same proposals and supervisors ticked and
            // the same message still typed. A redirect threw all of that away and re-ticked the
            // whole page, so a missing sentence cost the coordinator the batch they had built.
            async Task<IActionResult> RefuseAsync(string why)
            {
                var refused = await LoadDispatchScreenAsync(page, supervisorSearch, sort, desc, search);
                refused.ChosenProposalIds = proposalIds;
                refused.ChosenSupervisorIds = supervisorIds;
                refused.Comments = comments;
                refused.ChosenRespondBy = respondBy;

                TempData["ErrorMessage"] = why;
                return View(nameof(assigning_proposal_forsupervisor), refused);
            }

            if (proposalIds.Length == 0 || supervisorIds.Length == 0)
            {
                return await RefuseAsync("Choose at least one proposal and at least one supervisor.");
            }

            if (respondBy is null)
            {
                return await RefuseAsync("Say when the supervisors have to answer by.");
            }

            if (respondBy <= DateTime.Now)
            {
                return await RefuseAsync("The date supervisors have to answer by has already passed.");
            }

            // Sent as UTC, because that is what the API stores and compares against. The form is
            // filled in and read back in the reader's own time, so the conversion belongs here,
            // once, rather than in every screen that shows the date afterwards.
            var result = await proposalsApi.SendToSupervisorsAsync(
                new SendToSupervisorsRequestDto(proposalIds, supervisorIds, comments ?? string.Empty,
                    respondBy?.ToUniversalTime()));

            if (!result.Success)
            {
                return await RefuseAsync(result.ErrorMessage ?? "Could not send the proposals.");
            }

            var sentTo = supervisorIds.Length == 1
                ? "Sent to the supervisor."
                : $"Sent to {supervisorIds.Length} supervisors.";

            TempData["SuccessMessage"] = respondBy is { } deadline
                ? sentTo + $" They have until {deadline:dddd d MMMM yyyy, HH:mm} to answer."
                : sentTo;

            // A send that went through does redirect, so a refresh cannot repeat it, and it keeps
            // the coordinator where they were reading.
            return RedirectToAction(nameof(assigning_proposal_forsupervisor),
                new { page, supervisorSearch, sort, desc, search });
        }

        /// <summary>
        /// Proposals a supervisor has offered to take on, waiting for the coordinator to make the
        /// assignment official.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> select_a_proposal_forstudent(
            int page = 1, string? sort = null, bool desc = false, string? search = null)
        {
            var model = new SupervisorSelectionsViewModel
            {
                Sort = sort ?? "submitted",
                Descending = desc,
                Search = search
            };

            // One request for the whole screen. The proposals carry their student's name and the
            // supervisors' answers, and the API returns only the ones with an offer to allocate, so
            // this no longer fetches every publication in the department to find a handful.
            //
            // The search and the sort go with it rather than being applied to what comes back: the
            // row somebody is looking for is usually not on the page they are already holding.
            // Oldest first unless asked otherwise, like the dispatch queue: the student who has
            // been waiting longest for a supervisor belongs at the top, not buried under every
            // offer that has come in since.
            // What this screen offers depends on whether anybody was asked first.
            var proposalRules = await settingsApi.GetProposalsAsync();
            model.SupervisorsExpressInterest = proposalRules.Data?.SupervisorsExpressInterest ?? true;

            var available = model.SupervisorsExpressInterest
                ? []
                : (await usersApi.GetSupervisorsAsync()).Data?.Items ?? [];

            var proposals = await proposalsApi.GetForCoordinatorAsync(
                page, awaitingAllocation: true, sort: sort ?? "submitted", descending: desc, search: search);

            if (!proposals.Success)
            {
                TempData["ErrorMessage"] = proposals.ErrorMessage ?? "Could not load the proposals waiting on you.";
                model.LoadFailed = true;
                return View(model);
            }

            model.Items = [.. (proposals.Data?.Items ?? []).Select(p => new SupervisorSelectionItem
            {
                StudentName = p.StudentName,
                Proposal = new ProposalDto(p.Id, p.PublicationContainerId, p.Title, p.Abstract, p.Status, p.SubmittedAt),
                Invitations = p.Invitations,
                Available = available
            })];

            model.TotalCount = proposals.Data?.TotalCount ?? 0;
            model.Pager = Paging.PagerFor(proposals.Data, "Coordinator", nameof(select_a_proposal_forstudent),
                model.RouteValues());

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignSupervisor(Guid proposalId, Guid supervisorId, string? comments)
        {
            var result = await proposalsApi.AssignSupervisorAsync(
                proposalId, new AssignSupervisorRequestDto(supervisorId, comments ?? string.Empty));

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? "Supervisor assigned. The student can now start their ethics declaration."
                : result.ErrorMessage ?? "Could not assign the supervisor.";

            return RedirectToAction(nameof(select_a_proposal_forstudent));
        }

        /// <summary>
        /// Refuses the offers made on a proposal, so it stops being one the coordinator is
        /// choosing between. Turning one down while the student's others are still live changes
        /// nothing else. Only when nothing of theirs still has somebody willing does the whole set
        /// go back to Send proposals, and the message says so.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DiscardSelections(Guid proposalId, string? comments)
        {
            if (string.IsNullOrWhiteSpace(comments))
            {
                TempData["ErrorMessage"] =
                    "Say why you are turning these offers down. It is the only record of the decision.";
                return RedirectToAction(nameof(select_a_proposal_forstudent));
            }

            var result = await proposalsApi.DiscardSelectionsAsync(
                proposalId, new CommentsRequestDto(comments.Trim()));

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Could not discard the offers.";
                return RedirectToAction(nameof(select_a_proposal_forstudent));
            }

            var outcome = result.Data;
            TempData["SuccessMessage"] = outcome is { StudentHasNothingLeft: true }
                ? $"Nobody was willing to take on {outcome.StudentName}'s work, so all "
                  + $"{outcome.ProposalsReturned} of their proposals are back in Send proposals."
                : "Offers turned down. This student still has other proposals a supervisor is "
                  + "willing to take on.";

            return RedirectToAction(nameof(select_a_proposal_forstudent));
        }

        // ---------- Profile ----------

        // One profile screen for every role, rather than a copy per role.
        [HttpGet]
        public IActionResult staff_profile() => RedirectToAction("Me", "Profile");

        [HttpGet]
        public IActionResult Edit_staff_profile() => RedirectToAction("Me", "Profile");

        // ---------- Pipeline 2: ethics ----------

        /// <summary>
        /// The coordinator's first ethics screen. It covers two decisions that arrive at the same
        /// point in the workflow: confirming a supervisor's finding that no documentation is
        /// needed, and reviewing the documents once a supervisor has accepted them.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Ethic_review_aftersupervisor(
            Guid? id, int page = 1, string? sort = null, bool desc = false, string? search = null)
        {
            var (model, redirect) = await LoadEthicsQueueAsync(id, EthicsStage.AfterSupervisor, page, sort, desc, search);
            if (redirect is not null) return redirect;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmEthicsNotRequired(Guid id, bool requireDocumentation, string? comments)
        {
            var result = await ethicsApi.CoordinatorNotRequiredReviewAsync(
                id, new CoordinatorNotRequiredReviewRequestDto(requireDocumentation, comments ?? string.Empty));

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? requireDocumentation
                    ? "Recorded. The student has been asked to upload ethics documentation after all."
                    : "Ethics confirmed as not required. The student can now start their research paper."
                : result.ErrorMessage ?? "Could not record your decision.";

            return RedirectToAction(nameof(Ethic_review_aftersupervisor));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewEthicsDocuments(Guid id, bool approve, string? comments, Guid[]? documentIds = null)
        {
            // Nothing ticked means the whole set, which is what a coordinator who has singled none
            // of them out is saying. Ignored altogether when approving.
            var named = approve || documentIds is null ? [] : documentIds;

            var result = await ethicsApi.CoordinatorDocumentReviewAsync(
                id, new CoordinatorDocumentReviewRequestDto(approve, comments ?? string.Empty, named));

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? approve
                    ? "Approved. The Head of Department has been asked to review it."
                    : named.Length == 0
                        ? "Sent back to the student, who has been asked for all of the documents again."
                        : $"Sent back to the student, who has been asked for {named.Length} of the documents again."
                : result.ErrorMessage ?? "Could not record your review.";

            return RedirectToAction(nameof(Ethic_review_aftersupervisor));
        }

        /// <summary>
        /// The coordinator's closing decision on ethics, once the Head of Department has
        /// commented. Approving it verifies the ethics stage and opens the research paper.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Ethic_review_afters_headofdepartment(
            Guid? id, int page = 1, string? sort = null, bool desc = false, string? search = null)
        {
            var (model, redirect) = await LoadEthicsQueueAsync(id, EthicsStage.AfterHeadOfDepartment, page, sort, desc, search);
            if (redirect is not null) return redirect;

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinaliseEthics(Guid id, bool approve, string? comments, Guid[]? documentIds = null)
        {
            // Nothing ticked means the whole set, which is what a coordinator who has singled
            // none of them out is saying. Ignored altogether when approving.
            var named = approve || documentIds is null ? [] : documentIds;

            var result = await ethicsApi.CoordinatorFinalDecisionAsync(
                id, new CoordinatorFinalDecisionRequestDto(approve, comments ?? string.Empty, named));

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? approve
                    ? "Ethics approved. The student can now start their research paper."
                    : named.Length == 0
                        ? "Sent back to the student, who has been asked for all of the documents again."
                        : $"Sent back to the student, who has been asked for {named.Length} of the documents again."
                : result.ErrorMessage ?? "Could not record your decision.";

            return RedirectToAction(nameof(Ethic_review_afters_headofdepartment));
        }

        // ---------- Pipeline 3: the final decision on the paper ----------

        [HttpGet]
        public async Task<IActionResult> Evaluation_after_committee(
            int page = 1, string? sort = null, bool desc = false, string? search = null,
            int progressPage = 1, string? progressSort = null, bool progressDesc = false,
            string? progressSearch = null)
        {
            var model = new CoordinatorPapersViewModel
            {
                Sort = sort ?? "started",
                Descending = desc,
                Search = search,
                ProgressSort = progressSort ?? "started",
                ProgressDescending = progressDesc,
                ProgressSearch = progressSearch
            };

            // Two requests, one per list, rather than one split here. Whose turn it is decides which
            // list a row belongs to, and that is a question the database can answer, so each list
            // is now a page of its own. Split in the controller, neither could be paged: a page of
            // publications holds any number of rows for either list, or none.
            //
            // Oldest first by default on both, like every other queue.
            var ready = await containersApi.GetAllAsync(
                coordinatorId: CurrentUserId(), page: page,
                sort: sort ?? "started", descending: desc, search: search,
                paperAwaiting: RoleNames.Coordinator);

            if (!ready.Success)
            {
                TempData["ErrorMessage"] = ready.ErrorMessage ?? "Could not load your publications right now.";
                model.LoadFailed = true;
                return View(model);
            }

            foreach (var container in ready.Data?.Items ?? [])
            {
                var paper = await publicationsApi.GetByContainerAsync(container.Id);
                if (paper.Data is null) continue;

                var reviews = await publicationsApi.GetReviewsAsync(paper.Data.Id);

                model.ReadyForDecision.Add(new CoordinatorPaperItem
                {
                    Container = container,
                    Paper = paper.Data,
                    Reviews = reviews.Data ?? []
                });
            }

            model.DecisionTotal = ready.Data?.TotalCount ?? 0;
            model.DecisionPager = Paging.PagerFor(ready.Data, "Coordinator", nameof(Evaluation_after_committee),
                model.RouteValues().Where(v => v.Key != "page").ToDictionary(v => v.Key, v => v.Value));

            // The other list: papers under way that this coordinator is only watching. Everything
            // it shows is on the containers listing already, so a row nobody can act on here costs
            // no further requests.
            var moving = await containersApi.GetAllAsync(
                coordinatorId: CurrentUserId(), page: progressPage,
                sort: progressSort ?? "started", descending: progressDesc, search: progressSearch,
                paperAwaiting: "!" + RoleNames.Coordinator);

            // No filtering here. The API returns papers in flight that are somebody else's turn,
            // which is exactly this list, and filtering again after the page was cut would leave
            // the pager reporting a total it is not showing.
            model.InProgress = [.. (moving.Data?.Items ?? [])
                .Select(c => new CoordinatorPaperInProgress { Container = c })];

            model.ProgressTotal = moving.Data?.TotalCount ?? 0;
            model.ProgressPager = new PagerViewModel
            {
                Controller = "Coordinator",
                Action = nameof(Evaluation_after_committee),
                Page = moving.Data?.Page ?? 1,
                TotalPages = moving.Data?.TotalPages ?? 1,
                PageKey = "progressPage",
                RouteValues = model.RouteValues().Where(v => v.Key != "progressPage")
                    .ToDictionary(v => v.Key, v => v.Value)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DecideOnPaper(Guid publicationId, bool accept, string? comments)
        {
            var result = await publicationsApi.CoordinatorFinalDecisionAsync(
                publicationId, new PaperReviewDecisionRequestDto(accept, comments ?? string.Empty));

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? accept
                    ? "Accepted. The student now decides whether it goes into the public catalogue."
                    : "Sent back to the student with your comments."
                : result.ErrorMessage ?? "Could not record your decision.";

            return RedirectToAction(nameof(Evaluation_after_committee));
        }

        // Left over from the scaffold: an action that returned a view nobody ever wrote, so the
        // only thing it could do was throw. Appointing an evaluation committee is the
        // administrator's step and lives on AdminController, and nothing here linked to this.

        // ---------- Helpers ----------

        private enum EthicsStage { AfterSupervisor, AfterHeadOfDepartment }

        /// <summary>Everything a pager link has to carry: the ordering, plus whatever else is set.</summary>
        private static Dictionary<string, string?> Merge(
            Dictionary<string, string?> values, (string Key, string? Value)? extra)
        {
            if (extra is { } pair && !string.IsNullOrWhiteSpace(pair.Value)) values[pair.Key] = pair.Value;
            return values;
        }

        /// <summary>
        /// The publications waiting on the coordinator at one point of the ethics workflow, with
        /// the approval and documents for each. An optional id narrows it to a single one, so a
        /// link from the dashboard opens straight onto that publication.
        /// </summary>
        private async Task<(CoordinatorEthicsViewModel Model, IActionResult? Redirect)> LoadEthicsQueueAsync(
            Guid? containerId, EthicsStage stage, int page,
            string? sort = null, bool descending = false, string? search = null)
        {
            var model = new CoordinatorEthicsViewModel
            {
                Stage = stage.ToString(),
                Sort = sort ?? "started",
                Descending = descending,
                Search = search
            };

            // The API is asked for this screen's queue, by name. Both of the coordinator's ethics
            // decisions answer "waiting on the Coordinator", so a role was never enough to tell
            // them apart. The screens used to fetch every publication and read each approval's
            // timestamps to work it out, which meant a page of publications could hold any number
            // of rows for either screen, or none.
            var steps = stage == EthicsStage.AfterHeadOfDepartment
                ? EthicsSteps.CoordinatorFinalDecision
                : EthicsSteps.CoordinatorFirstReview;

            // Oldest first by default. An ethics queue is worked from the front, and a publication
            // that has been waiting a fortnight matters more than one that arrived this morning.
            var containers = await containersApi.GetAllAsync(
                coordinatorId: CurrentUserId(), ethicsSteps: steps, page: page,
                sort: sort ?? "started", descending: descending, search: search);

            if (!containers.Success)
            {
                TempData["ErrorMessage"] = containers.ErrorMessage ?? "Could not load your publications right now.";
                model.LoadFailed = true;
                return (model, null);
            }

            // The whole queue, not this page of it: the count above the list is what tells a
            // coordinator how much is waiting, and a figure capped at the page size would be wrong.
            model.TotalCount = containers.Data?.TotalCount ?? 0;

            // And how much is waiting on the other ethics screen. Asked for by size alone, because
            // the menu has one Ethics decisions entry for two queues and work in the other one
            // would otherwise sit unseen until somebody happened to look at the dashboard.
            var otherSteps = stage == EthicsStage.AfterHeadOfDepartment
                ? EthicsSteps.CoordinatorFirstReview
                : EthicsSteps.CoordinatorFinalDecision;

            var other = await containersApi.GetAllAsync(
                coordinatorId: CurrentUserId(), ethicsSteps: otherSteps, page: 1, pageSize: 1);
            model.OtherQueueCount = other.Data?.TotalCount ?? 0;

            var candidates = (containers.Data?.Items ?? []).ToList();

            if (containerId is { } only)
            {
                candidates = candidates.Where(c => c.Id == only).ToList();
                if (candidates.Count == 0)
                {
                    TempData["ErrorMessage"] = "That publication isn't waiting on you at this step.";
                    return (model, RedirectToAction(nameof(Coordinator_dashboard)));
                }
            }

            // Only the rows on this page are filled in, so the cost follows the page rather than
            // the department.
            foreach (var container in candidates)
            {
                var approval = await ethicsApi.GetApprovalAsync(container.Id);
                if (approval.Data is null) continue;

                var documents = await ethicsApi.GetDocumentsAsync(container.Id);

                // Only what the supervisor accepted. Reading a set is the supervisor's job, and
                // they have done it: the versions they sent back are already answered, and listing
                // them here asks this reader to work out which of five rows are the live three.
                var accepted = (documents.Data ?? [])
                    .Where(d => d.Status == EthicsDocumentStatus.Accepted)
                    .ToList();

                model.Items.Add(new CoordinatorEthicsItem
                {
                    Container = container,
                    Approval = approval.Data,
                    Documents = accepted
                });
            }

            model.Pager = Paging.PagerFor(containers.Data, "Coordinator",
                stage == EthicsStage.AfterHeadOfDepartment
                    ? nameof(Ethic_review_afters_headofdepartment)
                    : nameof(Ethic_review_aftersupervisor),
                // The ordering travels with the page number, or turning a page loses it.
                Merge(model.RouteValues(), containerId is null ? null : ("id", containerId.ToString())));

            return (model, null);
        }

        private Guid? CurrentUserId() =>
            Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;
    }
}
