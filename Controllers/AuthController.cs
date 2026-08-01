using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchPublicationManagementSystem.Common;
using ResearchPublicationManagementSystem.Infrastructure.Api;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;
using ResearchPublicationManagementSystem.Models;
using ResearchPublicationManagementSystem.Services;

namespace ResearchPublicationManagementSystem.Controllers
{
    public class AuthController(
        AuthApiClient authApiClient,
        DepartmentsApiClient departmentsApiClient,
        NotificationsApiClient notificationsApiClient,
        IInstitutionDetails institutionDetails,
        IAuthCookieService authCookieService) : Controller
    {
        // GET: /Auth/home
        [HttpGet]
        [AllowAnonymous]
        public IActionResult home(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToLandingPage();
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                ViewData["ReturnUrl"] = returnUrl;
                return View("home", model);
            }

            var result = await authApiClient.LoginAsync(new LoginRequestDto(model.Email, model.Password));
            if (!result.Success || result.Data is null)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Login failed.");
                ViewData["ReturnUrl"] = returnUrl;
                return View("home", model);
            }

            await authCookieService.SignInAsync(HttpContext, result.Data, model.RememberMe);
            await AnnounceUnreadNotificationsAsync();

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToLandingPage(result.Data.User.Roles);
        }

        // GET: /Auth/passwordrecovery
        [HttpGet]
        [AllowAnonymous]
        public IActionResult passwordrecovery() => View(new ForgotPasswordViewModel());

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> passwordrecovery(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The backend always returns success here regardless of whether the address is
            // registered (no email enumeration). Do not branch the UI on the result.
            await authApiClient.ForgotPasswordAsync(model.Email);

            ViewData["Submitted"] = true;
            return View(model);
        }

        // GET: /Auth/passwordreset
        [HttpGet]
        [AllowAnonymous]
        public IActionResult passwordreset(string? email = null, string? token = null)
        {
            return View(new ResetPasswordViewModel
            {
                Email = email ?? string.Empty,
                Token = token ?? string.Empty
            });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> passwordreset(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await authApiClient.ResetPasswordAsync(new ResetPasswordRequestDto(model.Email, model.Token, model.NewPassword));
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Could not reset your password.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Your password has been reset. You can now log in.";
            return RedirectToAction(nameof(home));
        }

        // GET: /Auth/signup
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> signup()
        {
            // Closed systems say so before the form, not after it has been filled in. The API
            // would refuse the registration either way; this is about not wasting someone's time.
            var institution = await institutionDetails.GetAsync();
            if (!institution.SelfRegistrationOpen)
            {
                return View("RegistrationClosed");
            }

            var model = new SignupViewModel();
            await PopulateDepartmentOptionsAsync(model);
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> signup(SignupViewModel model)
        {
            var institution = await institutionDetails.GetAsync();
            if (!institution.SelfRegistrationOpen)
            {
                return View("RegistrationClosed");
            }

            // The domain comes from settings: an institution that adds a second student domain
            // should not find this form still asking the old one for a student ID.
            var isStudentEmail = model.Email.EndsWith(institution.StudentEmailDomain, StringComparison.OrdinalIgnoreCase);
            if (isStudentEmail)
            {
                if (string.IsNullOrWhiteSpace(model.StudentIdNumber)) ModelState.AddModelError(nameof(model.StudentIdNumber), "Student ID is required.");
                if (string.IsNullOrWhiteSpace(model.Programme)) ModelState.AddModelError(nameof(model.Programme), "Programme is required.");
                if (string.IsNullOrWhiteSpace(model.Cohort)) ModelState.AddModelError(nameof(model.Cohort), "Cohort is required.");
                if (model.DepartmentId is null) ModelState.AddModelError(nameof(model.DepartmentId), "Department is required.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDepartmentOptionsAsync(model);
                return View(model);
            }

            var request = new RegisterRequestDto(
                model.Email,
                model.Password,
                model.FirstName,
                model.LastName,
                model.InstitutionalId,
                model.StudentIdNumber,
                model.Programme,
                model.Cohort,
                model.DepartmentId,
                null);

            var result = await authApiClient.RegisterAsync(request);
            if (!result.Success)
            {
                if (result.FieldErrors is not null)
                {
                    foreach (var (field, messages) in result.FieldErrors)
                    {
                        foreach (var message in messages) ModelState.AddModelError(field, message);
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Registration failed.");
                }

                await PopulateDepartmentOptionsAsync(model);
                return View(model);
            }

            return RedirectToAction(nameof(RegistrationPending));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult RegistrationPending() => View();

        // GET: /Auth/VerifyEmail?userId=..&token=..
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail(Guid userId, string token)
        {
            var result = await authApiClient.VerifyEmailAsync(userId, token);
            ViewData["Success"] = result.Success;
            ViewData["Message"] = result.Success
                ? "Your email has been verified. You can now log in."
                : result.ErrorMessage ?? "This verification link is invalid or has expired.";
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = User.FindFirst(AuthClaimTypes.RefreshToken)?.Value;
            if (refreshToken is not null)
            {
                await authApiClient.LogoutAsync(refreshToken);
            }

            await authCookieService.SignOutAsync(HttpContext);
            return RedirectToAction(nameof(home));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied() => View();

        private async Task PopulateDepartmentOptionsAsync(SignupViewModel model)
        {
            var result = await departmentsApiClient.GetAllAsync();
            model.DepartmentOptions = result.Success && result.Data is not null ? result.Data : [];
        }

        private IActionResult RedirectToLandingPage(IReadOnlyList<string>? roles = null)
        {
            var (controller, action) = roles is null ? RoleLanding.For(User) : RoleLanding.For(roles);
            return RedirectToAction(action, controller);
        }

        /// <summary>
        /// Tells someone, as they arrive, that something is waiting. The bell alone is easy to
        /// walk past, and with email switched off the application is the only place a
        /// notification ever appears.
        ///
        /// Runs after the sign-in cookie is issued, because the count is fetched with the token
        /// that cookie carries. Failure is silent: an unreachable count is not worth turning a
        /// successful sign-in into an error.
        /// </summary>
        private async Task AnnounceUnreadNotificationsAsync()
        {
            var unread = await notificationsApiClient.GetUnreadCountAsync();
            if (!unread.Success || unread.Data <= 0)
            {
                return;
            }

            TempData["InfoMessage"] = unread.Data == 1
                ? "You have 1 unread notification."
                : $"You have {unread.Data} unread notifications.";
        }
    }
}
