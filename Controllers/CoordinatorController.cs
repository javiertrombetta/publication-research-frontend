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

            model.Publications = containers.Data ?? [];

            var pending = await proposalsApi.GetPendingAsync();
            model.ProposalsAwaitingDispatch = pending.Data ?? [];

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

            var pending = await proposalsApi.GetPendingAsync();
            if (!pending.Success)
            {
                TempData["ErrorMessage"] = pending.ErrorMessage ?? "Could not load the proposals waiting to be sent.";
                model.LoadFailed = true;
                return View(model);
            }

            model.Proposals = pending.Data ?? [];

            var supervisors = await usersApi.GetSupervisorsAsync();
            model.Supervisors = (supervisors.Data ?? [])
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToList();

            // A proposal doesn't carry its student's name, so the coordinator's containers are
            // fetched once and matched up rather than one request per proposal.
            var containers = await containersApi.GetAllAsync(coordinatorId: CurrentUserId());
            model.Containers = containers.Data ?? [];

            // Paged by publication rather than by proposal: they are sent out per student, and a
            // page boundary that split one student's three proposals across two pages would make
            // the form on each of them wrong.
            var publications = model.Proposals.Select(p => p.PublicationContainerId).Distinct().ToList();
            var shown = Paging.Page(publications, page).ToHashSet();
            model.Proposals = [.. model.Proposals.Where(p => shown.Contains(p.PublicationContainerId))];
            model.Pager = new PagerViewModel
            {
                Controller = "Coordinator",
                Action = nameof(assigning_proposal_forsupervisor),
                Page = Paging.ClampPage(page, publications.Count),
                TotalPages = Paging.TotalPages(publications.Count)
            };

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

            var containers = await containersApi.GetAllAsync(coordinatorId: CurrentUserId());
            if (!containers.Success)
            {
                TempData["ErrorMessage"] = containers.ErrorMessage ?? "Could not load your publications right now.";
                model.LoadFailed = true;
                return View(model);
            }

            // Only publications still choosing a supervisor can have anything to act on.
            var candidates = (containers.Data ?? [])
                .Where(c => c.CurrentPipeline == PipelineStage.ResearchProposals && c.Status != "Completed")
                .ToList();

            // One request for every proposal and every supervisor's answer, joined to the
            // publications in memory. This used to be a request per publication and then one per
            // proposal on top, so the page grew more expensive with the department while showing
            // the same few rows anybody could act on.
            var proposals = await proposalsApi.GetForCoordinatorAsync();
            var byContainer = (proposals.Data ?? [])
                .ToLookup(p => p.PublicationContainerId);

            foreach (var container in candidates)
            {
                foreach (var proposal in byContainer[container.Id])
                {
                    // Nothing to decide until a supervisor has actually accepted one.
                    if (!proposal.Invitations.Any(i => i.IsSelected)) continue;

                    model.Items.Add(new SupervisorSelectionItem
                    {
                        Container = container,
                        Proposal = new ProposalDto(proposal.Id, proposal.PublicationContainerId,
                            proposal.Title, proposal.Abstract, proposal.Status, proposal.SubmittedAt),
                        Invitations = proposal.Invitations
                    });
                }
            }

            var total = model.Items.Count;
            model.Items = Paging.Page(model.Items, page);
            model.Pager = new PagerViewModel
            {
                Controller = "Coordinator",
                Action = nameof(select_a_proposal_forstudent),
                Page = Paging.ClampPage(page, total),
                TotalPages = Paging.TotalPages(total)
            };

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
            var (model, redirect) = await LoadEthicsQueueAsync(id, EthicsStage.AfterSupervisor);
            if (redirect is not null) return redirect;

            ApplyPaging(model, nameof(Ethic_review_aftersupervisor), id, page);
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
            var (model, redirect) = await LoadEthicsQueueAsync(id, EthicsStage.AfterHeadOfDepartment);
            if (redirect is not null) return redirect;

            ApplyPaging(model, nameof(Ethic_review_afters_headofdepartment), id, page);
            return View(model);
        }

        /// <summary>
        /// Cuts the queue down to one page. Shared by both ethics screens, which differ only in
        /// which decision they offer and are otherwise the same list.
        /// </summary>
        private static void ApplyPaging(CoordinatorEthicsViewModel model, string action, Guid? id, int page)
        {
            var total = model.Items.Count;
            model.Items = Paging.Page(model.Items, page);
            model.Pager = new PagerViewModel
            {
                Controller = "Coordinator",
                Action = action,
                Page = Paging.ClampPage(page, total),
                TotalPages = Paging.TotalPages(total),
                // Kept so that paging a screen opened on one publication stays on it.
                RouteValues = id is null ? [] : new() { ["id"] = id.ToString() }
            };
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
            // and this decision — so filtering on the status alone put a decision form in front of
            // the coordinator on three papers out of four that the API would then refuse.
            var papers = (containers.Data ?? [])
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
            Guid? containerId, EthicsStage stage)
        {
            var model = new CoordinatorEthicsViewModel { Stage = stage.ToString() };

            var containers = await containersApi.GetAllAsync(coordinatorId: CurrentUserId());
            if (!containers.Success)
            {
                TempData["ErrorMessage"] = containers.ErrorMessage ?? "Could not load your publications right now.";
                model.LoadFailed = true;
                return (model, null);
            }

            // Which of the coordinator's two ethics steps a publication is at is decided below,
            // from the approval itself — the listing only says the turn is theirs.
            var candidates = (containers.Data ?? [])
                .Where(c => c.EthicsAwaitingRole == RoleNames.Coordinator)
                .ToList();

            if (containerId is { } only)
            {
                candidates = candidates.Where(c => c.Id == only).ToList();
                if (candidates.Count == 0)
                {
                    TempData["ErrorMessage"] = "That publication isn't waiting on you at this step.";
                    return (model, RedirectToAction(nameof(Coordinator_dashboard)));
                }
            }

            foreach (var container in candidates)
            {
                var approval = await ethicsApi.GetApprovalAsync(container.Id);
                if (approval.Data is null) continue;

                // The closing decision only exists once the Head of Department has commented.
                var isFinalStep = approval.Data.HeadOfDepartmentReviewedAt is not null;
                if ((stage == EthicsStage.AfterHeadOfDepartment) != isFinalStep) continue;

                var documents = await ethicsApi.GetDocumentsAsync(container.Id);

                model.Items.Add(new CoordinatorEthicsItem
                {
                    Container = container,
                    Approval = approval.Data,
                    Documents = documents.Data ?? []
                });
            }

            return (model, null);
        }

        private Guid? CurrentUserId() =>
            Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;
    }
}
