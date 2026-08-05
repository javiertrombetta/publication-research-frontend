using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchPublicationManagementSystem.Common;
using ResearchPublicationManagementSystem.Infrastructure.Api;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;
using ResearchPublicationManagementSystem.Models;

namespace ResearchPublicationManagementSystem.Controllers
{
    /// <summary>
    /// The Head of Department oversees a department rather than individual publications, and has
    /// exactly one step in the workflow: commenting on a student's ethics documentation after the
    /// coordinator has approved it. They comment rather than decide. The coordinator closes the
    /// ethics stage afterwards, with these comments in front of them.
    /// </summary>
    [Authorize(Roles = RoleNames.HeadOfDepartment)]
    public class HeadOfDepartmentController(
        ContainersApiClient containersApi,
        ProposalsApiClient proposalsApi,
        EthicsApiClient ethicsApi,
        PublicationsApiClient publicationsApi) : Controller
    {
        // ---------- Overview ----------

        [HttpGet]
        public async Task<IActionResult> Head_of_Department_dashboard(
            int page = 1, string? sort = null, bool desc = false, string? search = null)
        {
            var model = new HeadOfDepartmentDashboardViewModel
            {
                Sort = sort ?? "started",
                Descending = desc,
                Search = search
            };

            var containers = await containersApi.GetInMyDepartmentAsync(
                page: page, sort: sort ?? "started", descending: desc, search: search);

            if (!containers.Success)
            {
                TempData["ErrorMessage"] = containers.ErrorMessage ?? "Could not load your department's publications.";
                model.LoadFailed = true;
                return View(model);
            }

            model.Publications = containers.Data?.Items ?? [];
            model.TotalCount = containers.Data?.TotalCount ?? 0;
            model.Pager = Paging.PagerFor(containers.Data, "HeadOfDepartment",
                nameof(Head_of_Department_dashboard), model.RouteValues());

            // How much of the department sits at each stage, each asked for as a count rather than
            // a page: the cards state figures, and a figure capped at the page size would be wrong.
            // Counted from the listing's own totals so the three add up to what is below them.
            var all = await containersApi.GetInMyDepartmentAsync(pageSize: 100);
            var everything = all.Data?.Items ?? [];

            model.ProposalStageTotal = everything.Count(c => c.CurrentPipeline == PipelineStage.ResearchProposals);
            model.EthicsStageTotal = everything.Count(c => c.CurrentPipeline == PipelineStage.EthicsApproval);
            model.PaperStageTotal = everything.Count(c => c.CurrentPipeline == PipelineStage.ResearchPaper);
            model.AwaitingMyReviewTotal = everything.Count(c => c.EthicsAwaitingRole == RoleNames.HeadOfDepartment);

            return View(model);
        }

        /// <summary>
        /// Ethics documentation waiting on this Head of Department. An optional id narrows it to
        /// one publication, so a link from the dashboard opens straight onto it.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Headofdepartment_feedback(
            Guid? id, int page = 1, string? sort = null, bool desc = false, string? search = null)
        {
            var model = new HeadOfDepartmentEthicsViewModel
            {
                Sort = sort ?? "started",
                Descending = desc,
                Search = search,
                OnlyId = id
            };

            List<PublicationContainerDto> candidates;

            if (id is { } only)
            {
                // Asked for by id, so it is fetched by id. Looking for it inside a page of the
                // queue found it only when it happened to be on the page the reader was on, and
                // told everybody else their own publication was not waiting on them.
                var one = await containersApi.GetByIdAsync(only);
                if (!one.Success || one.Data is null)
                {
                    TempData["ErrorMessage"] = "That publication isn't waiting on your review.";
                    return RedirectToAction(nameof(Head_of_Department_dashboard));
                }

                if (one.Data.EthicsAwaitingStep != EthicsSteps.HeadOfDepartmentReview)
                {
                    TempData["ErrorMessage"] = "That publication isn't waiting on your review.";
                    return RedirectToAction(nameof(Head_of_Department_dashboard));
                }

                candidates = [one.Data];
                model.TotalCount = 1;
            }
            else
            {
                // This screen's own queue, by name, one page of it. Everything else the department
                // has in flight is somebody else's problem and no longer travels down the wire.
                // Oldest first by default, as on every other queue: the longest wait is the one
                // that needs looking at.
                var containers = await containersApi.GetInMyDepartmentAsync(
                    ethicsSteps: EthicsSteps.HeadOfDepartmentReview, page: page,
                    sort: sort ?? "started", descending: desc, search: search);

                if (!containers.Success)
                {
                    TempData["ErrorMessage"] = containers.ErrorMessage ?? "Could not load your department's publications.";
                    model.LoadFailed = true;
                    return View(model);
                }

                candidates = [.. containers.Data?.Items ?? []];
                model.TotalCount = containers.Data?.TotalCount ?? 0;
                model.Pager = Paging.PagerFor(containers.Data, "HeadOfDepartment",
                    nameof(Headofdepartment_feedback), model.RouteValues());
            }

            // Only the rows on this page are filled in: each costs two further requests.
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

                model.Items.Add(new HeadOfDepartmentEthicsItem
                {
                    Container = container,
                    Approval = approval.Data,
                    Documents = accepted
                });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(Guid id, string comments)
        {
            // Whether a comment is required here is the institution's setting, not this screen's
            // rule. A check of its own refused a review the administrator had made optional, and
            // the API is the one place that reads the setting.
            var result = await ethicsApi.HeadOfDepartmentReviewAsync(
                id, new HeadOfDepartmentReviewRequestDto(comments));

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? "Review recorded. The coordinator will make the final ethics decision."
                : result.ErrorMessage ?? "Could not record your review.";

            return RedirectToAction(nameof(Headofdepartment_feedback));
        }

        // ---------- Department oversight ----------

        // ---------- One publication, whole, to read ----------

        /// <summary>
        /// Everything a publication in this department holds: its proposals, its ethics stage with
        /// the documents that were uploaded, its paper with every version and what the committee
        /// said, and the trail of who did what. Every file can be downloaded.
        ///
        /// Nothing here changes anything. A Head of Department oversees a department, and their one
        /// move in the workflow is commenting on ethics documentation, which has its own screen.
        /// This replaced two listings that each showed one stage of the same publications: reading
        /// one meant knowing in advance which of them to look in.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Publication(Guid id, int historyPage = 1, string? tab = null)
        {
            var container = await containersApi.GetByIdAsync(id);
            if (!container.Success || container.Data is null)
            {
                TempData["ErrorMessage"] = container.ErrorMessage ?? "Could not open that publication.";
                return RedirectToAction(nameof(Head_of_Department_dashboard));
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

                    var reviews = await publicationsApi.GetReviewsAsync(written.Id);
                    model.Reviews = reviews.Data ?? [];
                }
            }

            // Best-effort, as on every other screen that shows a trail: a publication is still
            // worth reading when its history cannot be.
            var history = await containersApi.GetActivityHistoryAsync(id, historyPage);
            model.History = history.Data?.Items ?? [];
            model.HistoryTotal = history.Data?.TotalCount ?? 0;
            model.HistoryPager = Paging.PagerFor(history.Data, "HeadOfDepartment", nameof(Publication),
                new Dictionary<string, string?> { ["id"] = id.ToString(), ["tab"] = "history" }, "historyPage");

            return View(model);
        }

        // ---------- Profile ----------

        [HttpGet]
        public IActionResult staff_profile() => RedirectToAction("Me", "Profile");

        [HttpGet]
        public IActionResult Edit_staff_profile() => RedirectToAction("Me", "Profile");

    }
}
