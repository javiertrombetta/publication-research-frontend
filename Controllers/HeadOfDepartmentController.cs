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
        EthicsApiClient ethicsApi) : Controller
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
        /// The research paper stage across the department, read-only.
        ///
        /// Their one decision is the ethics comment, and this is not it. It is oversight: a head of
        /// department is the authority over the department, so what is happening to its research
        /// and who is holding each piece up is theirs to see, even where the next move is somebody
        /// else's to make.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Department_papers(
            int page = 1, string? sort = null, bool desc = false, string? search = null)
        {
            var model = new DepartmentPapersViewModel
            {
                Sort = sort ?? "started",
                Descending = desc,
                Search = search
            };

            var containers = await containersApi.GetInMyDepartmentAsync(
                page: page, sort: sort ?? "started", descending: desc, search: search);

            if (!containers.Success)
            {
                TempData["ErrorMessage"] = containers.ErrorMessage ?? "Could not load your department's papers.";
                model.LoadFailed = true;
                return View(model);
            }

            // The paper stage only. Filtered here rather than asked for, because a container with
            // no paper yet has nothing to show on this screen and the API's paper filter answers a
            // narrower question: whose turn it is, not whether the stage has been reached.
            model.Publications = [.. (containers.Data?.Items ?? [])
                .Where(c => c.CurrentPipeline == PipelineStage.ResearchPaper)];

            model.TotalCount = containers.Data?.TotalCount ?? 0;
            model.Pager = Paging.PagerFor(containers.Data, "HeadOfDepartment",
                nameof(Department_papers), model.RouteValues());

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

                model.Items.Add(new HeadOfDepartmentEthicsItem
                {
                    Container = container,
                    Approval = approval.Data,
                    Documents = documents.Data ?? []
                });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(Guid id, string comments)
        {
            if (string.IsNullOrWhiteSpace(comments))
            {
                TempData["ErrorMessage"] = "Add your comments before submitting the review.";
                return RedirectToAction(nameof(Headofdepartment_feedback));
            }

            var result = await ethicsApi.HeadOfDepartmentReviewAsync(
                id, new HeadOfDepartmentReviewRequestDto(comments));

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? "Review recorded. The coordinator will make the final ethics decision."
                : result.ErrorMessage ?? "Could not record your review.";

            return RedirectToAction(nameof(Headofdepartment_feedback));
        }

        // ---------- Department oversight ----------

        /// <summary>
        /// Every proposal from students in the department. Read-only: the Head of Department is
        /// not part of the proposal workflow, but does need to see what their department is doing.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> all_proposals_fromstudent(
            int page = 1, string? sort = null, bool desc = false, string? search = null)
        {
            var model = new DepartmentProposalsViewModel
            {
                Sort = sort ?? "submitted",
                Descending = desc,
                Search = search
            };

            // One paged request for the whole screen. Each proposal carries its author's name and
            // its publication's id, so there is no second call to find out who wrote what, and the
            // API decides how many rows come back rather than the size of the department.
            var proposals = await proposalsApi.GetInMyDepartmentAsync(
                page, sort: sort ?? "submitted", descending: desc, search: search);
            if (!proposals.Success)
            {
                TempData["ErrorMessage"] = proposals.ErrorMessage ?? "Could not load your department's proposals.";
                model.LoadFailed = true;
                return View(model);
            }

            // One row per proposal, in the order the API returned them. Grouping them by student
            // undid the ordering the reader had asked for: a list sorted by title came back sorted
            // by title within each student and by nothing at all between them.
            model.Items = [.. (proposals.Data?.Items ?? [])
                .Select(p => new DepartmentProposalItem
                {
                    StudentName = p.StudentName,
                    Proposal = new ProposalDto(
                        p.Id, p.PublicationContainerId, p.Title, p.Abstract, p.Status, p.SubmittedAt)
                })];

            model.TotalCount = proposals.Data?.TotalCount ?? 0;
            model.Pager = Paging.PagerFor(proposals.Data, "HeadOfDepartment", nameof(all_proposals_fromstudent),
                model.RouteValues());

            return View(model);
        }

        // ---------- Profile ----------

        [HttpGet]
        public IActionResult staff_profile() => RedirectToAction("Me", "Profile");

        [HttpGet]
        public IActionResult Edit_staff_profile() => RedirectToAction("Me", "Profile");

    }
}
