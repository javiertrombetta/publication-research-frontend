using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchPublicationManagementSystem.Common;
using ResearchPublicationManagementSystem.Infrastructure.Api;
using ResearchPublicationManagementSystem.Models;

namespace ResearchPublicationManagementSystem.Controllers
{
    /// <summary>
    /// The institution-wide audit trail: every recorded action against every entity. Distinct
    /// from a publication's activity history, which is one publication's story told to the people
    /// working on it.
    /// </summary>
    [Authorize(Roles = RoleNames.Admin)]
    public class AuditLogsController(AdminApiClient adminApi) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] AuditLogQuery query)
        {
            query.Page = Math.Max(1, query.Page);
            query.PageSize = AuditLogQuery.DefaultPageSize;

            var model = new AuditLogViewModel { Query = query };

            var result = await adminApi.GetAuditLogAsync(query);
            if (!result.Success || result.Data is null)
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Could not load the audit log.";
                model.LoadFailed = true;
                return View(model);
            }

            model.Results = result.Data;
            return View(model);
        }
    }
}
