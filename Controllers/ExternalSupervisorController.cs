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
    /// Evaluation committee members. Reviewers and external members do exactly the same thing. The
    /// backend gives both the same two endpoints, so one controller serves both rather than a
    /// duplicate for each. (The name is the team's original; it now covers reviewers too.)
    ///
    /// A member reads the paper and records one decision. They cannot see other members' votes
    /// before deciding, which is the point of an evaluation committee.
    /// </summary>
    // Anyone who can be appointed to a committee can open these screens. Locking them to the two
    // committee-member roles meant a supervisor or coordinator could be appointed, be notified, and
    // then be refused the screen where the decision is made, so the committee could never reach the
    // number of approvals it needed.
    [Authorize(Roles = RoleNames.CommitteeEligibleRoles)]
    public class ExternalSupervisorController(
        CommitteesApiClient committeesApi) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> External_Supervisor_Dashboard(
            int page = 1, string? sort = null, bool desc = false)
        {
            var (model, failed) = await LoadAssignmentsAsync(page, sort, desc, action: nameof(External_Supervisor_Dashboard));

            if (!failed)
            {
                // How many are still theirs to vote on, counted by the API across everything
                // assigned to them. Worked out from the rows in hand it would have been a figure
                // about this page rather than about them, and it is the first thing the card says.
                var awaiting = await committeesApi.GetMyAssignmentsAsync(pageSize: 1, awaitingMe: true);
                model.AwaitingTotal = awaiting.Data?.TotalCount ?? 0;
            }

            return View(model);
        }

        /// <summary>
        /// The papers assigned to this member. An optional id narrows it to one, so a link from
        /// the dashboard opens straight onto that paper.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> committee_review(
            Guid? id, int page = 1, string? sort = null, bool desc = false, string? search = null)
        {
            var (model, failed) = await LoadAssignmentsAsync(page, sort, desc, search);

            if (!failed && id is { } only)
            {
                model.Items = model.Items.Where(i => i.Committee.Id == only).ToList();
                if (model.Items.Count == 0)
                {
                    TempData["ErrorMessage"] = "That paper isn't one of your committee assignments.";
                    return RedirectToAction(nameof(External_Supervisor_Dashboard));
                }
            }

            // Narrowed to one assignment, there is nothing to page through.
            if (id is not null) model.Pager = null;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitDecision(Guid committeeId, bool approve, string comments)
        {
            if (string.IsNullOrWhiteSpace(comments))
            {
                TempData["ErrorMessage"] = "Add your comments before recording a decision.";
                return RedirectToAction(nameof(committee_review));
            }

            var result = await committeesApi.MemberReviewAsync(
                committeeId, new CommitteeMemberReviewRequestDto(approve, comments));

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? approve
                    ? "Approval recorded."
                    : "Your objection has been recorded."
                : result.ErrorMessage ?? "Could not record your decision.";

            return RedirectToAction(nameof(committee_review));
        }

        // ---------- Profile ----------

        [HttpGet]
        public IActionResult staff_profile() => RedirectToAction("Me", "Profile");

        [HttpGet]
        public IActionResult Edit_staff_profile() => RedirectToAction("Me", "Profile");

        // ---------- Helpers ----------

        /// <param name="action">
        /// Which of the two screens is asking. Both list the same assignments, and a pager built
        /// for the wrong one turns the page on a screen the reader is not looking at.
        /// </param>
        private async Task<(CommitteeDashboardViewModel Model, bool Failed)> LoadAssignmentsAsync(
            int page = 1, string? sort = null, bool desc = false, string? search = null,
            string action = nameof(committee_review))
        {
            var model = new CommitteeDashboardViewModel
            {
                Sort = sort, Descending = desc, Search = search, Action = action
            };

            var assignments = await committeesApi.GetMyAssignmentsAsync(
                page, sort: sort, descending: desc, search: search);
            if (!assignments.Success)
            {
                TempData["ErrorMessage"] = assignments.ErrorMessage ?? "Could not load your committee assignments.";
                model.LoadFailed = true;
                return (model, true);
            }

            model.TotalCount = assignments.Data?.TotalCount ?? 0;
            model.Pager = Paging.PagerFor(assignments.Data, "ExternalSupervisor", action, model.RouteValues());

            var me = CurrentUserId();

            foreach (var committee in assignments.Data?.Items ?? [])
            {
                // The assignment now carries the paper's title and abstract, which is all this
                // list shows of it. It used to fetch the paper per committee, so a member on
                // several committees paid a request for each before the page appeared.
                model.Items.Add(new CommitteeAssignmentItem
                {
                    Committee = committee,
                    Me = committee.Members.FirstOrDefault(m => m.UserId == me)
                });
            }

            return (model, false);
        }

        private Guid? CurrentUserId() =>
            Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;
    }
}
