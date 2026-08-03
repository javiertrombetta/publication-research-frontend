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
        public async Task<IActionResult> SupervisorDashboard(
            int page = 1, string? sort = null, bool desc = false, string? search = null)
        {
            var model = new SupervisorDashboardViewModel
            {
                // Spelled out rather than left null so the heading in force is the one marked as
                // active. Oldest first, as on every other queue.
                Sort = sort ?? "started",
                Descending = desc,
                Search = search
            };

            var supervising = await containersApi.GetSupervisingAsync(
                page: page, sort: sort ?? "started", descending: desc, search: search);

            if (!supervising.Success)
            {
                TempData["ErrorMessage"] = supervising.ErrorMessage ?? "Could not load your publications right now.";
                model.LoadFailed = true;
                return View(model);
            }

            model.Supervising = supervising.Data?.Items ?? [];
            model.TotalCount = supervising.Data?.TotalCount ?? 0;
            model.Pager = Paging.PagerFor(supervising.Data, "Supervisor", nameof(SupervisorDashboard),
                model.RouteValues());

            // The three cards, each asked only for its size. A page of rows would be thrown away:
            // what the top of this screen says is how much is waiting, and the listing below says
            // which publication it belongs to.
            var proposals = await proposalsApi.GetInvitedAsync(page: 1, pageSize: 1);
            model.ProposalsToReviewTotal = proposals.Data?.TotalCount ?? 0;

            // The two ethics queues counted separately, because a ruling and a document check are
            // different work even though they arrive together.
            var ruling = await containersApi.GetSupervisingAsync(
                ethicsSteps: EthicsSteps.SupervisorDecision, page: 1, pageSize: 1);
            model.EthicsAwaitingRulingTotal = ruling.Data?.TotalCount ?? 0;

            var check = await containersApi.GetSupervisingAsync(
                ethicsSteps: EthicsSteps.SupervisorDocumentReview, page: 1, pageSize: 1);
            model.EthicsAwaitingCheckTotal = check.Data?.TotalCount ?? 0;

            var papers = await publicationsApi.GetPendingForSupervisorAsync(page: 1, pageSize: 1);
            model.PapersToReviewTotal = papers.Data?.TotalCount ?? 0;

            return View(model);
        }

        // ---------- Pipeline 1: choosing a proposal ----------

        [HttpGet]
        public async Task<IActionResult> proposal_review(
            int page = 1, string? sort = null, bool desc = false, string? search = null)
        {
            var model = new InvitedProposalsViewModel { Sort = sort, Descending = desc, Search = search };

            var invited = await proposalsApi.GetInvitedAsync(page, sort: sort, descending: desc, search: search);
            if (!invited.Success)
            {
                TempData["ErrorMessage"] = invited.ErrorMessage ?? "Could not load the proposals sent to you.";
                model.LoadFailed = true;
                return View(model);
            }

            model.Proposals = invited.Data?.Items ?? [];
            model.TotalCount = invited.Data?.TotalCount ?? 0;
            model.Pager = Paging.PagerFor(invited.Data, "Supervisor", nameof(proposal_review), model.RouteValues());

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
        /// Every ethics stage waiting on this supervisor, both kinds in one list.
        ///
        /// Until now the only way in was the dashboard, which meant the work existed but had no
        /// screen of its own and nothing in the menu pointed at it. The two decisions live together
        /// because the question is "what is mine to do", and each row opens whichever screen its
        /// own decision needs.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Ethic_reviews(
            int page = 1, string? sort = null, bool desc = false, string? search = null)
        {
            var model = new SupervisorEthicsQueueViewModel
            {
                Sort = sort ?? "started",
                Descending = desc,
                Search = search
            };

            var queue = await containersApi.GetSupervisingAsync(
                ethicsSteps: EthicsSteps.SupervisorReview, page: page,
                sort: sort ?? "started", descending: desc, search: search);

            if (!queue.Success)
            {
                TempData["ErrorMessage"] = queue.ErrorMessage ?? "Could not load the ethics reviews waiting on you.";
                model.LoadFailed = true;
                return View(model);
            }

            model.Items = queue.Data?.Items ?? [];
            model.TotalCount = queue.Data?.TotalCount ?? 0;
            model.Pager = Paging.PagerFor(queue.Data, "Supervisor", nameof(Ethic_reviews), model.RouteValues());

            return View(model);
        }

        /// <summary>
        /// The supervisor's ethics screen for one publication. Which decision it offers depends on
        /// where the approval has got to. The requirement ruling comes first, the document check
        /// only once the student has uploaded them.
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

            // A decision that did not go through leaves the person on the screen they made it on,
            // with what they typed still worth retyping and the reason in front of them. Sending
            // them to the dashboard reads as though something worked.
            return result.Success
                ? RedirectToAction(nameof(Ethic_reviews))
                : RedirectToAction(nameof(Review_Ethic_assessmentchecklist), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Ethic_document_review(Guid id)
        {
            var (model, redirect) = await LoadEthicsAsync(id);
            return redirect ?? View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitDocumentReview(
            Guid id, bool accept, string comments, Guid[]? documentIds = null)
        {
            // Which documents are going back. None ticked means all of them, which is what the
            // button says when nothing is chosen.
            var result = await ethicsApi.SupervisorReviewAsync(
                id, new DocumentReviewDecisionRequestDto(accept, comments ?? string.Empty, documentIds));

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? accept
                    ? "Documents accepted. They now go to the coordinator."
                    : documentIds is { Length: > 0 }
                        ? $"Sent back. The student has been asked for {documentIds.Length} {(documentIds.Length == 1 ? "document" : "documents")} again, with your comments."
                        : "Sent back to the student with your comments."
                : result.ErrorMessage ?? "Could not record your review.";

            return result.Success
                ? RedirectToAction(nameof(Ethic_reviews))
                : RedirectToAction(nameof(Ethic_document_review), new { id });
        }

        // ---------- Pipeline 3: the research paper ----------

        [HttpGet]
        public async Task<IActionResult> publication_review(
            int page = 1, string? sort = null, bool desc = false, string? search = null)
        {
            var model = new SupervisorPapersViewModel { Sort = sort, Descending = desc, Search = search };

            var papers = await publicationsApi.GetPendingForSupervisorAsync(
                page, sort: sort, descending: desc, search: search);

            if (!papers.Success)
            {
                TempData["ErrorMessage"] = papers.ErrorMessage ?? "Could not load the papers awaiting your review.";
                model.LoadFailed = true;
                return View(model);
            }

            model.Papers = papers.Data?.Items ?? [];
            model.TotalCount = papers.Data?.TotalCount ?? 0;
            model.Pager = Paging.PagerFor(papers.Data, "Supervisor", nameof(publication_review), model.RouteValues());

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
        /// to. The API enforces that too. This keeps a wrong id from rendering a broken page.
        /// </summary>
        private async Task<(SupervisorEthicsViewModel Model, IActionResult? Redirect)> LoadEthicsAsync(Guid containerId)
        {
            // Asked for by id, not searched for in the supervising list. That list is paged, so
            // searching it answered "is this on the first page of what I supervise", which turned
            // supervisors away from their own eleventh publication onwards. The API applies the
            // access rule itself, so a refusal from it is the real answer.
            var supervising = await containersApi.GetByIdAsync(containerId);
            var container = supervising.Data;

            if (!supervising.Success || container is null)
            {
                TempData["ErrorMessage"] = "That publication isn't one of yours to supervise.";
                return (new SupervisorEthicsViewModel(), RedirectToAction(nameof(SupervisorDashboard)));
            }

            var model = new SupervisorEthicsViewModel { Container = container };

            var approval = await ethicsApi.GetApprovalAsync(containerId);
            model.Approval = approval.Data;

            var documents = await ethicsApi.GetDocumentsAsync(containerId);
            model.Documents = documents.Data ?? [];

            // What was asked for, so the screen can show a document that never arrived as well as
            // the ones that did.
            var required = await ethicsApi.GetRequiredDocumentsAsync(containerId);
            model.Required = required.Data ?? [];

            return (model, null);
        }
    }
}
