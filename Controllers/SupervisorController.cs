using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchPublicationManagementSystem.Common;
using ResearchPublicationManagementSystem.Infrastructure.Api;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;
using ResearchPublicationManagementSystem.Models;

namespace ResearchPublicationManagementSystem.Controllers
{
    /// <summary>
    /// The supervisor is the academic judgement in the process: they choose which proposal they
    /// are willing to supervise, rule on whether the research needs ethics approval, check the
    /// documents when it does, and review the finished paper.
    /// </summary>
    [Authorize(Roles = RoleNames.Supervisor)]
    public class SupervisorController(
        ContainersApiClient containersApi,
        ProposalsApiClient proposalsApi,
        EthicsApiClient ethicsApi,
        PublicationsApiClient publicationsApi) : Controller
    {
        // ---------- Overview ----------

        [HttpGet]
        public async Task<IActionResult> SupervisorDashboard()
        {
            var model = new SupervisorDashboardViewModel();

            var supervising = await containersApi.GetSupervisingAsync();
            if (!supervising.Success)
            {
                TempData["ErrorMessage"] = supervising.ErrorMessage ?? "Could not load your publications right now.";
                model.LoadFailed = true;
                return View(model);
            }

            model.Supervising = supervising.Data ?? [];

            // The listing already carries the ethics status, so the two ethics queues come out of
            // it without a request per publication.
            model.EthicsAwaitingDecision = model.Supervising
                .Where(c => c.EthicsStatus == EthicsStatus.PendingSupervisorDecision)
                .ToList();

            // Not "status is PendingVerification": that also covers documents this supervisor
            // has already accepted and passed on. The backend says whose turn it is.
            model.EthicsAwaitingReview = model.Supervising
                .Where(c => c.EthicsAwaitingRole == RoleNames.Supervisor
                            && c.EthicsStatus == EthicsStatus.PendingVerification)
                .ToList();

            var invited = await proposalsApi.GetInvitedAsync();
            model.InvitedProposals = invited.Data ?? [];

            var papers = await publicationsApi.GetPendingForSupervisorAsync();
            model.PapersAwaitingReview = papers.Data ?? [];

            return View(model);
        }

        // ---------- Pipeline 1: choosing a proposal ----------

        [HttpGet]
        public async Task<IActionResult> proposal_review()
        {
            var model = new InvitedProposalsViewModel();

            var invited = await proposalsApi.GetInvitedAsync();
            if (!invited.Success)
            {
                TempData["ErrorMessage"] = invited.ErrorMessage ?? "Could not load the proposals sent to you.";
                model.LoadFailed = true;
                return View(model);
            }

            model.Proposals = invited.Data ?? [];
            return View(model);
        }

        /// <summary>
        /// Says this supervisor is willing to take the proposal on. It is an offer, not the
        /// assignment: the coordinator still makes that official.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptProposal(Guid proposalId, string? comments)
        {
            var result = await proposalsApi.SupervisorSelectionAsync(
                proposalId, new SupervisorSelectionRequestDto(comments));

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? "Recorded. The coordinator will confirm the assignment."
                : result.ErrorMessage ?? "Could not record your answer.";

            return RedirectToAction(nameof(proposal_review));
        }

        // ---------- Pipeline 2: ethics ----------

        /// <summary>
        /// The supervisor's ethics screen for one publication. Which decision it offers depends
        /// on where the approval has got to — the requirement ruling comes first, the document
        /// check only once the student has uploaded them.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Review_Ethic_assessmentchecklist(Guid id)
        {
            var (model, redirect) = await LoadEthicsAsync(id);
            return redirect ?? View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitEthicsDecision(Guid id, bool isRequired, string comments)
        {
            var result = await ethicsApi.SupervisorDecisionAsync(
                id, new SupervisorRequirementDecisionRequestDto(isRequired, comments ?? string.Empty));

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? isRequired
                    ? "Recorded. The student has been asked to upload their ethics documentation."
                    : "Recorded. The coordinator will confirm that no documentation is required."
                : result.ErrorMessage ?? "Could not record your decision.";

            return RedirectToAction(nameof(SupervisorDashboard));
        }

        [HttpGet]
        public async Task<IActionResult> Ethic_document_review(Guid id)
        {
            var (model, redirect) = await LoadEthicsAsync(id);
            return redirect ?? View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitDocumentReview(Guid id, bool accept, string comments)
        {
            var result = await ethicsApi.SupervisorReviewAsync(
                id, new DocumentReviewDecisionRequestDto(accept, comments ?? string.Empty));

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? accept
                    ? "Documents accepted. They now go to the coordinator."
                    : "Sent back to the student with your comments."
                : result.ErrorMessage ?? "Could not record your review.";

            return RedirectToAction(nameof(SupervisorDashboard));
        }

        // ---------- Pipeline 3: the research paper ----------

        [HttpGet]
        public async Task<IActionResult> publication_review(int page = 1)
        {
            var model = new SupervisorPapersViewModel();

            var papers = await publicationsApi.GetPendingForSupervisorAsync();
            if (!papers.Success)
            {
                TempData["ErrorMessage"] = papers.ErrorMessage ?? "Could not load the papers awaiting your review.";
                model.LoadFailed = true;
                return View(model);
            }

            var all = papers.Data ?? [];
            model.Papers = Paging.Page(all, page);
            model.Pager = new PagerViewModel
            {
                Controller = "Supervisor",
                Action = nameof(publication_review),
                Page = Paging.ClampPage(page, all.Count),
                TotalPages = Paging.TotalPages(all.Count)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitPaperReview(Guid publicationId, bool accept, string comments)
        {
            var result = await publicationsApi.SupervisorReviewAsync(
                publicationId, new PaperReviewDecisionRequestDto(accept, comments ?? string.Empty));

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? accept
                    ? "Accepted. The paper goes on to the evaluation committee."
                    : "Sent back to the student with your comments."
                : result.ErrorMessage ?? "Could not record your review.";

            return RedirectToAction(nameof(publication_review));
        }

        // ---------- Profile ----------

        [HttpGet]
        public IActionResult staff_profile() => RedirectToAction("Me", "Profile");

        [HttpGet]
        public IActionResult Edit_staff_profile() => RedirectToAction("Me", "Profile");


        // ---------- Helpers ----------

        /// <summary>
        /// Loads one publication's ethics stage, refusing anything this supervisor isn't assigned
        /// to. The API enforces that too — this keeps a wrong id from rendering a broken page.
        /// </summary>
        private async Task<(SupervisorEthicsViewModel Model, IActionResult? Redirect)> LoadEthicsAsync(Guid containerId)
        {
            var supervising = await containersApi.GetSupervisingAsync();
            var container = (supervising.Data ?? []).FirstOrDefault(c => c.Id == containerId);

            if (container is null)
            {
                TempData["ErrorMessage"] = "That publication isn't one of yours to supervise.";
                return (new SupervisorEthicsViewModel(), RedirectToAction(nameof(SupervisorDashboard)));
            }

            var model = new SupervisorEthicsViewModel { Container = container };

            var approval = await ethicsApi.GetApprovalAsync(containerId);
            model.Approval = approval.Data;

            var documents = await ethicsApi.GetDocumentsAsync(containerId);
            model.Documents = documents.Data ?? [];

            return (model, null);
        }
    }
}
