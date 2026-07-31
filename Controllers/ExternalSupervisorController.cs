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
    /// Evaluation committee members. Internal and external members do exactly the same thing —
    /// the backend gives both the same two endpoints — so one controller serves both rather than
    /// a duplicate for each. (The name is the team's original; it now covers internal members
    /// too.)
    ///
    /// A member reads the paper and records one decision. They cannot see other members' votes
    /// before deciding, which is the point of an evaluation committee.
    /// </summary>
    [Authorize(Roles = $"{RoleNames.InternalCommitteeMember},{RoleNames.ExternalCommitteeMember}")]
    public class ExternalSupervisorController(
        CommitteesApiClient committeesApi,
        PublicationsApiClient publicationsApi) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> External_Supervisor_Dashboard()
        {
            var (model, _) = await LoadAssignmentsAsync();
            return View(model);
        }

        /// <summary>
        /// The papers assigned to this member. An optional id narrows it to one, so a link from
        /// the dashboard opens straight onto that paper.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> committee_review(Guid? id)
        {
            var (model, failed) = await LoadAssignmentsAsync();

            if (!failed && id is { } only)
            {
                model.Items = model.Items.Where(i => i.Committee.Id == only).ToList();
                if (model.Items.Count == 0)
                {
                    TempData["ErrorMessage"] = "That paper isn't one of your committee assignments.";
                    return RedirectToAction(nameof(External_Supervisor_Dashboard));
                }
            }

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

        private async Task<(CommitteeDashboardViewModel Model, bool Failed)> LoadAssignmentsAsync()
        {
            var model = new CommitteeDashboardViewModel();

            var assignments = await committeesApi.GetMyAssignmentsAsync();
            if (!assignments.Success)
            {
                TempData["ErrorMessage"] = assignments.ErrorMessage ?? "Could not load your committee assignments.";
                model.LoadFailed = true;
                return (model, true);
            }

            var me = CurrentUserId();

            foreach (var committee in assignments.Data ?? [])
            {
                // The assignment carries only a publication id, so the paper is fetched to have
                // something readable to review.
                var paper = await publicationsApi.GetByIdAsync(committee.PublicationId);

                model.Items.Add(new CommitteeAssignmentItem
                {
                    Committee = committee,
                    Paper = paper.Data,
                    Me = committee.Members.FirstOrDefault(m => m.UserId == me)
                });
            }

            return (model, false);
        }

        private Guid? CurrentUserId() =>
            Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;
    }
}
