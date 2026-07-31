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
    /// coordinator has approved it. They comment rather than decide — the coordinator closes the
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

            var containers = await containersApi.GetInMyDepartmentAsync();
            if (!containers.Success)
            {
                TempData["ErrorMessage"] = containers.ErrorMessage ?? "Could not load your department's publications.";
                model.LoadFailed = true;
                return View(model);
            }

            model.Publications = containers.Data ?? [];
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

            var containers = await containersApi.GetInMyDepartmentAsync();
            if (!containers.Success)
            {
                TempData["ErrorMessage"] = containers.ErrorMessage ?? "Could not load your department's publications.";
                model.LoadFailed = true;
                return View(model);
            }

            var candidates = (containers.Data ?? [])
                .Where(c => c.EthicsAwaitingRole == RoleNames.HeadOfDepartment)
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

            // Paged before the details are fetched, not after: each row costs two requests to
            // fill in, so a department with fifty waiting used to pay a hundred of them to render
            // ten. Now the page decides how many are asked for.
            var total = candidates.Count;
            model.Pager = new PagerViewModel
            {
                Controller = "HeadOfDepartment",
                Action = nameof(Headofdepartment_feedback),
                Page = Paging.ClampPage(page, total),
                TotalPages = Paging.TotalPages(total),
                RouteValues = id is null ? [] : new() { ["id"] = id.ToString() }
            };

            foreach (var container in Paging.Page(candidates, page))
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

            var containers = await containersApi.GetInMyDepartmentAsync();
            if (!containers.Success)
            {
                TempData["ErrorMessage"] = containers.ErrorMessage ?? "Could not load your department's publications.";
                model.LoadFailed = true;
                return View(model);
            }

            // The department's proposals in one request rather than one per publication: this
            // screen lists a whole department, so the old shape made it the slowest page in the
            // system by a distance.
            var proposals = await proposalsApi.GetInMyDepartmentAsync();
            var byContainer = (proposals.Data ?? []).ToLookup(p => p.PublicationContainerId);

            foreach (var container in containers.Data ?? [])
            {
                var forContainer = byContainer[container.Id]
                    .Select(p => new ProposalDto(p.Id, p.PublicationContainerId, p.Title, p.Abstract, p.Status, p.SubmittedAt))
                    .ToList();

                if (forContainer.Count == 0) continue;

                model.Items.Add(new DepartmentProposalItem
                {
                    Container = container,
                    Proposals = forContainer
                });
            }

            var total = model.Items.Count;
            model.Items = Paging.Page(model.Items, page);
            model.Pager = new PagerViewModel
            {
                Controller = "HeadOfDepartment",
                Action = nameof(all_proposals_fromstudent),
                Page = Paging.ClampPage(page, total),
                TotalPages = Paging.TotalPages(total)
            };

            return View(model);
        }

        // ---------- Profile ----------

        [HttpGet]
        public IActionResult staff_profile() => RedirectToAction("Me", "Profile");

        [HttpGet]
        public IActionResult Edit_staff_profile() => RedirectToAction("Me", "Profile");

    }
}
