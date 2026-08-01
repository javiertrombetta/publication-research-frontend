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
        UsersApiClient usersApi) : Controller
    {
        // ---------- Overview ----------

        [HttpGet]
        public async Task<IActionResult> Coordinator_dashboard()
        {
            var model = new CoordinatorDashboardViewModel();

            // Scoped to this coordinator: the endpoint would happily return every container in
            // the institution, which is not what a coordinator's queue means.
            var containers = await containersApi.GetAllAsync(coordinatorId: CurrentUserId());
            if (!containers.Success)
            {
                TempData["ErrorMessage"] = containers.ErrorMessage ?? "Could not load your publications right now.";
                model.LoadFailed = true;
                return View(model);
            }

            model.Publications = containers.Data?.Items ?? [];
            model.PublicationsTotal = containers.Data?.TotalCount ?? 0;

            var pending = await proposalsApi.GetPendingAsync();
            model.ProposalsAwaitingDispatch = pending.Data?.Items ?? [];
            model.ProposalsAwaitingDispatchTotal = pending.Data?.TotalCount ?? 0;

            return View(model);
        }

        // ---------- Pipeline 1: sending proposals out and assigning a supervisor ----------

        /// <summary>
        /// Submitted proposals waiting to go to supervisors, and the supervisors they can be sent
        /// to. Proposals go out as a batch, because a supervisor is choosing between them.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> assigning_proposal_forsupervisor(int page = 1)
        {
            var model = new AssignProposalsViewModel();

            var pending = await proposalsApi.GetPendingAsync(page);
            if (!pending.Success)
            {
                TempData["ErrorMessage"] = pending.ErrorMessage ?? "Could not load the proposals waiting to be sent.";
                model.LoadFailed = true;
                return View(model);
            }

            model.Proposals = pending.Data?.Items ?? [];

            var supervisors = await usersApi.GetSupervisorsAsync();
            model.Supervisors = (supervisors.Data ?? [])
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToList();

            // A proposal doesn't carry its student's name, so the coordinator's containers are
            // fetched once and matched up rather than one request per proposal.
            var containers = await containersApi.GetAllAsync(coordinatorId: CurrentUserId());
            model.Containers = containers.Data?.Items ?? [];

            model.Pager = Paging.PagerFor(pending.Data, "Coordinator", nameof(assigning_proposal_forsupervisor));

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendToSupervisors(Guid[] proposalIds, Guid[] supervisorIds, string? comments)
        {
            if (proposalIds.Length == 0 || supervisorIds.Length == 0)
            {
                TempData["ErrorMessage"] = "Choose at least one proposal and at least one supervisor.";
                return RedirectToAction(nameof(assigning_proposal_forsupervisor));
            }

            var result = await proposalsApi.SendToSupervisorsAsync(
                new SendToSupervisorsRequestDto(proposalIds, supervisorIds, comments ?? string.Empty));

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Could not send the proposals.";
                return RedirectToAction(nameof(assigning_proposal_forsupervisor));
            }

            TempData["SuccessMessage"] = supervisorIds.Length == 1
                ? "Sent to the supervisor."
                : $"Sent to {supervisorIds.Length} supervisors.";

            return RedirectToAction(nameof(assigning_proposal_forsupervisor));
        }

        /// <summary>
        /// Proposals a supervisor has offered to take on, waiting for the coordinator to make the
        /// assignment official.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> select_a_proposal_forstudent(int page = 1)
        {
            var model = new SupervisorSelectionsViewModel();

            // One request for the whole screen. The proposals carry their student's name and the
            // supervisors' answers, and the API returns only the ones with an offer to allocate, so
            // this no longer fetches every publication in the department to find a handful.
            var proposals = await proposalsApi.GetForCoordinatorAsync(page, awaitingAllocation: true);
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
                Invitations = p.Invitations
            })];

            model.Pager = Paging.PagerFor(proposals.Data, "Coordinator", nameof(select_a_proposal_forstudent));

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
        public async Task<IActionResult> Ethic_review_aftersupervisor(Guid? id, int page = 1)
        {
            var (model, redirect) = await LoadEthicsQueueAsync(id, EthicsStage.AfterSupervisor, page);
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
        public async Task<IActionResult> ReviewEthicsDocuments(Guid id, bool approve, string? comments)
        {
            var result = await ethicsApi.CoordinatorDocumentReviewAsync(
                id, new CoordinatorDocumentReviewRequestDto(approve, comments ?? string.Empty));

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? approve
                    ? "Approved. The Head of Department has been asked to review it."
                    : "Sent back to the student with your comments."
                : result.ErrorMessage ?? "Could not record your review.";

            return RedirectToAction(nameof(Ethic_review_aftersupervisor));
        }

        /// <summary>
        /// The coordinator's closing decision on ethics, once the Head of Department has
        /// commented. Approving it verifies the ethics stage and opens the research paper.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Ethic_review_afters_headofdepartment(Guid? id, int page = 1)
        {
            var (model, redirect) = await LoadEthicsQueueAsync(id, EthicsStage.AfterHeadOfDepartment, page);
            if (redirect is not null) return redirect;

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinaliseEthics(Guid id, bool approve, string? comments)
        {
            var result = await ethicsApi.CoordinatorFinalDecisionAsync(
                id, new CoordinatorFinalDecisionRequestDto(approve, comments ?? string.Empty));

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? approve
                    ? "Ethics approved. The student can now start their research paper."
                    : "Sent back to the student with your comments."
                : result.ErrorMessage ?? "Could not record your decision.";

            return RedirectToAction(nameof(Ethic_review_afters_headofdepartment));
        }

        // ---------- Pipeline 3: the final decision on the paper ----------

        [HttpGet]
        public async Task<IActionResult> Evaluation_after_committee()
        {
            var model = new CoordinatorPapersViewModel();

            var containers = await containersApi.GetAllAsync(coordinatorId: CurrentUserId());
            if (!containers.Success)
            {
                TempData["ErrorMessage"] = containers.ErrorMessage ?? "Could not load your publications right now.";
                model.LoadFailed = true;
                return View(model);
            }

            // Split on whose turn it is rather than on the paper's status. UnderReview covers the
            // supervisor still reading it, an admin appointing a committee, the committee voting
            // and this decision, so filtering on the status alone put a decision form in front of
            // the coordinator on three papers out of four that the API would then refuse.
            var papers = (containers.Data?.Items ?? [])
                .Where(c => c.PaperStatus is PublicationStatus.UnderReview
                                         or PublicationStatus.Resubmitted
                                         or PublicationStatus.RevisionsRequested)
                .ToList();

            foreach (var container in papers)
            {
                if (container.PaperAwaitingRole != RoleNames.Coordinator)
                {
                    // Everything this row shows is already on the containers listing, so a paper
                    // nobody can act on here costs no further requests.
                    model.InProgress.Add(new CoordinatorPaperInProgress { Container = container });
                    continue;
                }

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


        [HttpGet]
        public IActionResult assigning_committee_members() => View();

        // ---------- Helpers ----------

        private enum EthicsStage { AfterSupervisor, AfterHeadOfDepartment }

        /// <summary>
        /// The publications waiting on the coordinator at one point of the ethics workflow, with
        /// the approval and documents for each. An optional id narrows it to a single one, so a
        /// link from the dashboard opens straight onto that publication.
        /// </summary>
        private async Task<(CoordinatorEthicsViewModel Model, IActionResult? Redirect)> LoadEthicsQueueAsync(
            Guid? containerId, EthicsStage stage, int page)
        {
            var model = new CoordinatorEthicsViewModel { Stage = stage.ToString() };

            // The API is asked for this screen's queue, by name. Both of the coordinator's ethics
            // decisions answer "waiting on the Coordinator", so a role was never enough to tell
            // them apart. The screens used to fetch every publication and read each approval's
            // timestamps to work it out, which meant a page of publications could hold any number
            // of rows for either screen, or none.
            var steps = stage == EthicsStage.AfterHeadOfDepartment
                ? EthicsSteps.CoordinatorFinalDecision
                : EthicsSteps.CoordinatorFirstReview;

            var containers = await containersApi.GetAllAsync(
                coordinatorId: CurrentUserId(), ethicsSteps: steps, page: page);

            if (!containers.Success)
            {
                TempData["ErrorMessage"] = containers.ErrorMessage ?? "Could not load your publications right now.";
                model.LoadFailed = true;
                return (model, null);
            }

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

                model.Items.Add(new CoordinatorEthicsItem
                {
                    Container = container,
                    Approval = approval.Data,
                    Documents = documents.Data ?? []
                });
            }

            model.Pager = Paging.PagerFor(containers.Data, "Coordinator",
                stage == EthicsStage.AfterHeadOfDepartment
                    ? nameof(Ethic_review_afters_headofdepartment)
                    : nameof(Ethic_review_aftersupervisor),
                containerId is null ? null : new() { ["id"] = containerId.ToString() });

            return (model, null);
        }

        private Guid? CurrentUserId() =>
            Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;
    }
}
