using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchPublicationManagementSystem.Common;
using ResearchPublicationManagementSystem.Infrastructure.Api;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;
using ResearchPublicationManagementSystem.Models;

namespace ResearchPublicationManagementSystem.Controllers
{
    /// <summary>
    /// The user directory. An administrator grants operational roles. An account signs itself up
    /// and waits here until someone decides what it is allowed to do, and enables, disables or
    /// triggers a password reset.
    /// </summary>
    [Authorize(Roles = RoleNames.Admin)]
    public class UsersController(UsersApiClient usersApi, DepartmentsApiClient departmentsApi) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index(
            string? role, string? status, string? search,
            int page = 1, string? sort = null, bool desc = false)
        {
            var model = new UserDirectoryViewModel
            {
                Role = role, Status = status, Search = search, Sort = sort, Descending = desc
            };

            var result = await usersApi.GetAllAsync(role, status, search, page, sort: sort, descending: desc);
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Could not load the user directory.";
                model.LoadFailed = true;
                return View(model);
            }

            model.Users = result.Data?.Items ?? [];
            model.TotalCount = result.Data?.TotalCount ?? 0;
            model.Pager = Paging.PagerFor(result.Data, "Users", nameof(Index), model.RouteValues());

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var result = await usersApi.GetByIdAsync(id);
            if (!result.Success || result.Data is null)
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "That account could not be found.";
                return RedirectToAction(nameof(Index));
            }

            var departments = await departmentsApi.GetAllAsync();

            return View(new UserDetailViewModel
            {
                User = result.Data,
                Departments = departments.Data ?? [],
                CurrentDepartmentIds = result.Data.DepartmentIds ?? []
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(
            Guid id, string role, string? comments, Guid? departmentId, string? affiliation,
            Guid[]? departmentIds)
        {
            // Two shapes because the roles differ: one department for a coordinator or a head,
            // several for a supervisor or a reviewer, none for an external committee member. The
            // form shows whichever the chosen role takes, and the API refuses a role granted
            // without the departments it needs.
            var result = await usersApi.ChangeRoleAsync(id, new ChangeUserRoleRequestDto(
                role, comments ?? string.Empty, departmentId, affiliation,
                departmentIds is { Length: > 0 } ? departmentIds : null));

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? $"Role changed to {DisplayText.Humanise(role)}."
                : result.ErrorMessage ?? "Could not change the role.";

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetEnabled(Guid id, bool enabled, string? comments)
        {
            var request = new CommentsRequestDto(comments ?? string.Empty);

            var result = enabled
                ? await usersApi.EnableAsync(id, request)
                : await usersApi.DisableAsync(id, request);

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? enabled ? "Account enabled." : "Account disabled."
                : result.ErrorMessage ?? "Could not change the account's status.";

            return RedirectToAction(nameof(Details), new { id });
        }

        /// <summary>
        /// Sends the account a password-reset email. No password is set here. An administrator
        /// never chooses someone else's password.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(Guid id, string? comments)
        {
            var result = await usersApi.ResetPasswordAsync(id, new CommentsRequestDto(comments ?? string.Empty));

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? "A password-reset email has been sent to this account."
                : result.ErrorMessage ?? "Could not start the password reset.";

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(Guid id, string firstName, string lastName, string? institutionalId, string? comments)
        {
            var result = await usersApi.UpdateAsync(id,
                new UpdateUserRequestDto(firstName, lastName, institutionalId, comments ?? string.Empty));

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? "Account updated."
                : result.ErrorMessage ?? "Could not update the account.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // ---------- Creating an account ----------

        /// <summary>
        /// For staff who will not sign themselves up. The account is created already verified,
        /// so it skips the email-confirmation step a self-registered account goes through.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            return View(await BuildCreateModelAsync(new CreateUserRequestDto()));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Role))
            {
                TempData["ErrorMessage"] = "An email address and a role are required.";
                return View(await BuildCreateModelAsync(request));
            }

            var result = await usersApi.CreateAsync(request);
            if (!result.Success || result.Data is null)
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Could not create the account.";
                AddApiErrors(result);
                return View(await BuildCreateModelAsync(request));
            }

            TempData["SuccessMessage"] = $"Account created for {request.Email}.";
            return RedirectToAction(nameof(Details), new { id = result.Data.Id });
        }

        // ---------- Deleting an account ----------

        /// <summary>
        /// Administrators only, and a reason is required. It is what the audit trail will carry.
        /// What the backend does is strip the account of personal data and lock it out rather than
        /// remove the row: every reference to a user is a Restrict foreign key, so a real row
        /// delete would either be refused or would have to detach a publication from its author.
        /// The person can no longer sign in and is no longer identifiable; what they did remains
        /// attributable.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, string comments)
        {
            if (string.IsNullOrWhiteSpace(comments))
            {
                TempData["ErrorMessage"] = "A reason is required before an account can be deleted.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var result = await usersApi.DeleteAsync(id, new CommentsRequestDto(comments));
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Could not delete the account.";
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["SuccessMessage"] = "Account deleted. The audit trail keeps a record of it.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Helpers ----------

        private async Task<CreateUserViewModel> BuildCreateModelAsync(CreateUserRequestDto request)
        {
            var departments = await departmentsApi.GetAllAsync();
            return new CreateUserViewModel
            {
                Request = request,
                Departments = departments.Data ?? []
            };
        }

        /// <summary>Surfaces field-level validation from the API next to the field it belongs to.</summary>
        private void AddApiErrors<T>(ApiResult<T> result)
        {
            foreach (var (field, messages) in result.FieldErrors ?? new Dictionary<string, string[]>())
            {
                foreach (var message in messages)
                {
                    ModelState.AddModelError(field, message);
                }
            }
        }
    }
}
