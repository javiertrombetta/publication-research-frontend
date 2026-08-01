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
        public async Task<IActionResult> Head_of_Department_dashboard()
        {
            var model = new HeadOfDepartmentDashboardViewModel();

            // The dashboard is an overview of the department, so it asks for a generous page and
            // states its figures from the total rather than from what fits on one.
            var containers = await containersApi.GetInMyDepartmentAsync(pageSize: 100);
            if (!containers.Success)
            {
                TempData["ErrorMessage"] = containers.ErrorMessage ?? "Could not load your department's publications.";
                model.LoadFailed = true;
                return View(model);
            }

            model.Publications = containers.Data?.Items ?? [];
            return View(model);
        }

        // ---------- Ethics review ----------

        /// <summary>
        /// Ethics documentation waiting on this Head of Department. An optional id narrows it to
        /// one publication, so a link from the dashboard opens straight onto it.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Headofdepartment_feedback(Guid? id, int page = 1)
        {
            var model = new HeadOfDepartmentEthicsViewModel();

            // This screen's own queue, by name, one page of it. Everything else the department has
            // in flight is somebody else's problem and no longer travels down the wire.
            var containers = await containersApi.GetInMyDepartmentAsync(
                ethicsSteps: EthicsSteps.HeadOfDepartmentReview, page: page);
            if (!containers.Success)
            {
                TempData["ErrorMessage"] = containers.ErrorMessage ?? "Could not load your department's publications.";
                model.LoadFailed = true;
                return View(model);
            }

            var candidates = (containers.Data?.Items ?? [])
                .ToList();

            if (id is { } only)
            {
                candidates = candidates.Where(c => c.Id == only).ToList();
                if (candidates.Count == 0)
                {
                    TempData["ErrorMessage"] = "That publication isn't waiting on your review.";
                    return RedirectToAction(nameof(Head_of_Department_dashboard));
                }
            }

            // Only the rows on this page are filled in: each costs two further requests.
            model.Pager = Paging.PagerFor(containers.Data, "HeadOfDepartment", nameof(Headofdepartment_feedback),
                id is null ? null : new() { ["id"] = id.ToString() });

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
        public async Task<IActionResult> all_proposals_fromstudent(int page = 1)
        {
            var model = new DepartmentProposalsViewModel();

            // One paged request for the whole screen. Each proposal carries its author's name and
            // its publication's id, so there is no second call to find out who wrote what, and the
            // API decides how many rows come back rather than the size of the department.
            var proposals = await proposalsApi.GetInMyDepartmentAsync(page);
            if (!proposals.Success)
            {
                TempData["ErrorMessage"] = proposals.ErrorMessage ?? "Could not load your department's proposals.";
                model.LoadFailed = true;
                return View(model);
            }

            // Still grouped by publication, since that is how a reader makes sense of them: three
            // proposals from one student are one decision, not three.
            model.Items = [.. (proposals.Data?.Items ?? [])
                .GroupBy(p => p.PublicationContainerId)
                .Select(group => new DepartmentProposalItem
                {
                    StudentName = group.First().StudentName,
                    Proposals = [.. group.Select(p => new ProposalDto(
                        p.Id, p.PublicationContainerId, p.Title, p.Abstract, p.Status, p.SubmittedAt))]
                })];

            model.Pager = Paging.PagerFor(proposals.Data, "HeadOfDepartment", nameof(all_proposals_fromstudent));

            return View(model);
        }

        // ---------- Profile ----------

        [HttpGet]
        public IActionResult staff_profile() => RedirectToAction("Me", "Profile");

        [HttpGet]
        public IActionResult Edit_staff_profile() => RedirectToAction("Me", "Profile");

    }
}
