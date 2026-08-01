using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchPublicationManagementSystem.Common;
using ResearchPublicationManagementSystem.Infrastructure.Api;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;
using ResearchPublicationManagementSystem.Models;

namespace ResearchPublicationManagementSystem.Controllers
{
    /// <summary>
    /// A student may run several publications at the same time, each with its own proposals,
    /// ethics workflow and paper. Every pipeline action therefore takes the publication's
    /// container id, and is guarded by both ownership and pipeline stage.
    /// </summary>
    [Authorize(Roles = RoleNames.Student)]
    public class StudentController(
        ContainersApiClient containersApi,
        ProposalsApiClient proposalsApi,
        EthicsApiClient ethicsApi,
        PublicationsApiClient publicationsApi,
        UsersApiClient usersApi) : Controller
    {
        private static readonly JsonSerializerOptions ProfileJsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // ---------- Listing ----------

        /// <summary>
        /// The student's publications, optionally filtered and sorted. Both live in the query
        /// string so a filtered view can be linked to or reloaded, and the whole thing keeps
        /// working without JavaScript.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> student_dashboard(string? q = null, string? sort = null)
        {
            var result = await containersApi.GetMineAsync(pageSize: 100);
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Could not load your publications right now.";
                return View(new StudentDashboardViewModel { LoadFailed = true, Query = q, Sort = NormaliseSort(sort) });
            }

            var all = result.Data?.Items ?? [];
            var model = new StudentDashboardViewModel
            {
                TotalCount = all.Count,
                Query = q,
                Sort = NormaliseSort(sort)
            };

            model.Publications = SortPublications(FilterPublications(all, q), model.Sort);
            return View(model);
        }

        private static string NormaliseSort(string? sort) => sort switch
        {
            PublicationSort.DateOldest => PublicationSort.DateOldest,
            PublicationSort.Title => PublicationSort.Title,
            PublicationSort.Status => PublicationSort.Status,
            _ => PublicationSort.DateNewest
        };

        /// <summary>
        /// Matches the title the student sees plus the people involved, which is what a
        /// publication is recognisable by on this screen.
        /// </summary>
        private static IReadOnlyList<PublicationContainerDto> FilterPublications(
            IReadOnlyList<PublicationContainerDto> source, string? query)
        {
            if (string.IsNullOrWhiteSpace(query)) return source;

            var term = query.Trim();

            bool Matches(string? value) =>
                value is not null && value.Contains(term, StringComparison.CurrentCultureIgnoreCase);

            return source
                .Where(p => Matches(p.DisplayTitle) || Matches(p.CoordinatorName) || Matches(p.AssignedSupervisorName))
                .ToList();
        }

        private static IReadOnlyList<PublicationContainerDto> SortPublications(
            IReadOnlyList<PublicationContainerDto> source, string sort) => sort switch
            {
                PublicationSort.DateOldest =>
                    source.OrderBy(p => p.CreatedAt).ToList(),

                PublicationSort.Title =>
                    source.OrderBy(p => p.DisplayTitle, StringComparer.CurrentCultureIgnoreCase).ToList(),

                // Finished ones last, otherwise by how far through the workflow they are.
                PublicationSort.Status =>
                    source.OrderBy(p => p.Status == "Completed" ? 1 : 0)
                          .ThenBy(p => p.CurrentPipeline)
                          .ThenBy(p => p.DisplayTitle, StringComparer.CurrentCultureIgnoreCase)
                          .ToList(),

                _ => source.OrderByDescending(p => p.CreatedAt).ToList()
            };

        // ---------- One publication ----------

        [HttpGet]
        public async Task<IActionResult> Publication(Guid id)
        {
            var (container, redirect) = await GetOwnedContainerAsync(id);
            if (redirect is not null) return redirect;

            var model = new PublicationDetailViewModel { Container = container! };

            var proposalsResult = await proposalsApi.GetByContainerAsync(id);
            model.Proposals = proposalsResult.Data ?? [];

            if (container!.CurrentPipeline >= PipelineStage.EthicsApproval)
            {
                var ethicsResult = await ethicsApi.GetApprovalAsync(id);
                if (ethicsResult.Success) model.EthicsApproval = ethicsResult.Data;
            }

            if (container.CurrentPipeline >= PipelineStage.ResearchPaper)
            {
                var pubResult = await publicationsApi.GetByContainerAsync(id);
                if (pubResult.Success) model.Publication = pubResult.Data;
            }

            // Best-effort: a publication is still perfectly usable if its history can't be read,
            // so a failure here shows an empty history tab rather than taking down the page.
            var historyResult = await containersApi.GetActivityHistoryAsync(id);
            model.History = historyResult.Data ?? [];

            return View(model);
        }

        // ---------- Creating a publication ----------

        [HttpGet]
        public IActionResult CreateContainer() => View();

        [HttpPost]
        [ActionName(nameof(CreateContainer))]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateContainerConfirmed()
        {
            var result = await containersApi.CreateAsync();
            if (!result.Success || result.Data is null)
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Could not start a new publication.";
                return View(nameof(CreateContainer));
            }

            TempData["SuccessMessage"] = "Publication started. Submit your research proposals to continue.";
            return RedirectToAction(nameof(Publication), new { id = result.Data.Id });
        }

        /// <summary>
        /// Discards a publication the student created by mistake. Only possible while it still
        /// has no proposals — the backend enforces that rule and is the authority here.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePublication(Guid id)
        {
            var (_, redirect) = await GetOwnedContainerAsync(id);
            if (redirect is not null) return redirect;

            var result = await containersApi.DeleteAsync(id);
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Could not delete this publication.";
                return RedirectToAction(nameof(Publication), new { id });
            }

            TempData["SuccessMessage"] = "Publication deleted.";
            return RedirectToAction(nameof(student_dashboard));
        }

        /// <summary>
        /// The last step: once the paper has been accepted, its author decides whether it appears
        /// in the public catalogue. Either answer closes the publication — declining is a real
        /// choice, not a postponement — so the view asks for confirmation before posting here.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishDecision(Guid id, bool publish)
        {
            var (_, redirect) = await GetOwnedContainerAsync(id, PipelineStage.ResearchPaper);
            if (redirect is not null) return redirect;

            var publicationResult = await publicationsApi.GetByContainerAsync(id);
            if (!publicationResult.Success || publicationResult.Data is null)
            {
                TempData["ErrorMessage"] = publicationResult.ErrorMessage ?? "Could not load your research paper.";
                return RedirectToAction(nameof(Publication), new { id });
            }

            // Comments are only required of someone deciding on the student's behalf, so the
            // student's own decision sends none and the backend records its default wording.
            var result = await publicationsApi.PublishDecisionAsync(
                publicationResult.Data.Id, new PublishDecisionRequestDto(publish, null));

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Could not record your publication decision.";
                return RedirectToAction(nameof(Publication), new { id });
            }

            TempData["SuccessMessage"] = publish
                ? "Your research paper is now in the public catalogue."
                : "Your research paper has been kept out of the public catalogue.";

            return RedirectToAction(nameof(Publication), new { id });
        }

        // ---------- Pipeline 1: proposals ----------

        [HttpGet]
        public async Task<IActionResult> Create_proposals(Guid id)
        {
            var (container, redirect) = await GetOwnedContainerAsync(id);
            if (redirect is not null) return redirect;

            var proposalsResult = await proposalsApi.GetByContainerAsync(id);
            var proposals = proposalsResult.Data ?? [];

            var model = new CreateProposalsViewModel { ContainerId = container!.Id };
            for (var i = 0; i < proposals.Count && i < model.Slots.Count; i++)
            {
                model.Slots[i] = new ProposalSlotViewModel
                {
                    ProposalId = proposals[i].Id,
                    Title = proposals[i].Title,
                    Abstract = proposals[i].Abstract,
                    Status = proposals[i].Status
                };
            }

            model.IsLocked = proposals.Any(p => p.Status is not ("Draft" or "Rejected"));
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create_proposals(CreateProposalsViewModel model, string action = "save")
        {
            var (_, redirect) = await GetOwnedContainerAsync(model.ContainerId);
            if (redirect is not null) return redirect;

            var firstSlot = model.Slots.Count > 0 ? model.Slots[0] : null;
            if (firstSlot is null || string.IsNullOrWhiteSpace(firstSlot.Title) || string.IsNullOrWhiteSpace(firstSlot.Abstract))
            {
                ModelState.AddModelError(string.Empty, "At least the first proposal's title and abstract are required.");
                return View(model);
            }

            foreach (var slot in model.Slots)
            {
                if (string.IsNullOrWhiteSpace(slot.Title) && string.IsNullOrWhiteSpace(slot.Abstract)) continue;

                var request = new SaveProposalRequestDto(slot.Title ?? string.Empty, slot.Abstract ?? string.Empty);
                var result = slot.ProposalId is { } existingId
                    ? await proposalsApi.UpdateAsync(existingId, request)
                    : await proposalsApi.CreateAsync(model.ContainerId, request);

                if (!result.Success || result.Data is null)
                {
                    AddApiErrors(result, "Could not save one of your proposals.");
                    return View(model);
                }

                slot.ProposalId = result.Data.Id;
                slot.Status = result.Data.Status;
            }

            if (action == "finish")
            {
                var finishResult = await proposalsApi.FinishSubmissionAsync(model.ContainerId);
                if (!finishResult.Success)
                {
                    AddApiErrors(finishResult, "Could not submit your proposals.");
                    return View(model);
                }

                TempData["SuccessMessage"] = "Proposals submitted.";
                return RedirectToAction(nameof(Publication), new { id = model.ContainerId });
            }

            TempData["SuccessMessage"] = "Draft saved.";
            return RedirectToAction(nameof(Create_proposals), new { id = model.ContainerId });
        }

        // ---------- Pipeline 2: ethics ----------

        [HttpGet]
        public async Task<IActionResult> Ethic_risk_assessment(Guid id)
        {
            var (container, redirect) = await GetOwnedContainerAsync(id, PipelineStage.EthicsApproval);
            if (redirect is not null) return redirect;

            var guidanceResult = await ethicsApi.GetGuidanceAsync();
            return View(new EthicsDeclarationPageViewModel
            {
                ContainerId = container!.Id,
                Guidance = guidanceResult.Success ? guidanceResult.Data : null
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ethic_risk_assessment(EthicsDeclarationPageViewModel model)
        {
            var (_, redirect) = await GetOwnedContainerAsync(model.ContainerId, PipelineStage.EthicsApproval);
            if (redirect is not null) return redirect;

            if (!ModelState.IsValid)
            {
                await RepopulateGuidanceAsync(model);
                return View(model);
            }

            var result = await ethicsApi.SubmitDeclarationAsync(model.ContainerId, model.Response!);
            if (!result.Success)
            {
                AddApiErrors(result, "Could not record your declaration.");
                await RepopulateGuidanceAsync(model);
                return View(model);
            }

            TempData["SuccessMessage"] = "Your ethics declaration has been recorded.";
            return RedirectToAction(nameof(Publication), new { id = model.ContainerId });
        }

        [HttpGet]
        public async Task<IActionResult> Upload_Ethic_file(Guid id)
        {
            var (container, redirect) = await GetOwnedContainerAsync(id, PipelineStage.EthicsApproval);
            if (redirect is not null) return redirect;

            var required = await ethicsApi.GetRequiredDocumentsAsync(id);
            var docsResult = await ethicsApi.GetDocumentsAsync(id);

            return View(new UploadEthicsDocumentsViewModel
            {
                ContainerId = container!.Id,
                Required = required.Data ?? [],
                ExistingDocuments = docsResult.Data ?? []
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 100_000_000)]
        public async Task<IActionResult> Upload_Ethic_file(UploadEthicsDocumentsViewModel model)
        {
            var (_, redirect) = await GetOwnedContainerAsync(model.ContainerId, PipelineStage.EthicsApproval);
            if (redirect is not null) return redirect;

            // The requirements are re-read rather than trusted from the form: what the API will
            // accept is what this publication was asked for, and a posted key that is not on that
            // list has no business being uploaded.
            var required = await ethicsApi.GetRequiredDocumentsAsync(model.ContainerId);
            var requirements = required.Data ?? [];

            var anyUploaded = false;
            foreach (var requirement in requirements)
            {
                if (!model.Files.TryGetValue(requirement.RequirementId, out var file) ||
                    file is not { Length: > 0 })
                {
                    continue;
                }

                var result = await ethicsApi.UploadDocumentAsync(
                    model.ContainerId, requirement.RequirementId.ToString(), file);

                if (!result.Success)
                {
                    AddApiErrors(result, $"Could not upload the {requirement.Name}.");
                }
                else
                {
                    anyUploaded = true;
                }
            }

            if (!anyUploaded && ModelState.ErrorCount == 0)
            {
                ModelState.AddModelError(string.Empty, "Choose at least one file to upload.");
            }

            if (!ModelState.IsValid)
            {
                var docsResult = await ethicsApi.GetDocumentsAsync(model.ContainerId);
                model.Required = requirements;
                model.ExistingDocuments = docsResult.Data ?? [];
                return View(model);
            }

            TempData["SuccessMessage"] = "Documents uploaded.";
            return RedirectToAction(nameof(Upload_Ethic_file), new { id = model.ContainerId });
        }

        // ---------- Pipeline 3: research paper ----------

        [HttpGet]
        public async Task<IActionResult> Create_Publication(Guid id)
        {
            var (container, redirect) = await GetOwnedContainerAsync(id, PipelineStage.ResearchPaper);
            if (redirect is not null) return redirect;

            var draftResult = await publicationsApi.GetOrCreateDraftAsync(id);
            if (!draftResult.Success || draftResult.Data is null)
            {
                TempData["ErrorMessage"] = draftResult.ErrorMessage
                    ?? "The research paper stage isn't available yet — finish the ethics process first.";
                return RedirectToAction(nameof(Publication), new { id });
            }

            var pub = draftResult.Data;
            var versionsResult = await publicationsApi.GetVersionsAsync(pub.Id);
            var versions = versionsResult.Data ?? [];
            var latestVersion = versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();

            return View(new CreatePublicationViewModel
            {
                ContainerId = container!.Id,
                PublicationId = pub.Id,
                Status = pub.Status,
                Title = pub.Title,
                Abstract = pub.Abstract,
                PublicationType = pub.PublicationType,
                PublicationYear = pub.PublicationYear,
                KeywordsCsv = string.Join(",", pub.Keywords),
                HasUploadedVersion = versions.Count > 0,
                LatestVersionNumber = latestVersion?.VersionNumber ?? 0,
                // So a paper past editing can still be read: the file is the paper, and being told
                // it was accepted without being able to open it is not seeing it.
                LatestVersionId = latestVersion?.Id
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(200_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 200_000_000)]
        public async Task<IActionResult> Create_Publication(CreatePublicationViewModel model, string action = "draft")
        {
            var (_, redirect) = await GetOwnedContainerAsync(model.ContainerId, PipelineStage.ResearchPaper);
            if (redirect is not null) return redirect;

            // The status comes back from the form, so it is the student's word for what their paper
            // was — good enough to stop the screen offering a save it cannot make, not to authorise
            // one. The API is what actually refuses editing a paper under review.
            if (!model.IsEditable)
            {
                TempData["ErrorMessage"] = "This paper is no longer yours to change — it has been submitted.";
                return RedirectToAction(nameof(Publication), new { id = model.ContainerId });
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var keywords = (model.KeywordsCsv ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var metadataResult = await publicationsApi.UpdateMetadataAsync(model.PublicationId, new UpdatePublicationMetadataRequestDto(
                model.Title, model.Abstract, model.PublicationType, model.PublicationYear, keywords, null));

            if (!metadataResult.Success)
            {
                AddApiErrors(metadataResult, "Could not save your changes.");
                return View(model);
            }

            if (model.ResearchFile is { Length: > 0 })
            {
                var uploadResult = await publicationsApi.UploadVersionAsync(model.PublicationId, model.ResearchFile, null, model.ReviewerNotes);
                if (!uploadResult.Success)
                {
                    AddApiErrors(uploadResult, "Could not upload your file.");
                    return View(model);
                }
            }

            if (action == "submit")
            {
                var submitResult = await publicationsApi.SubmitAsync(model.PublicationId);
                if (!submitResult.Success)
                {
                    AddApiErrors(submitResult, "Could not submit your research paper.");
                    return View(model);
                }

                TempData["SuccessMessage"] = "Your research paper has been submitted for review.";
                return RedirectToAction(nameof(Publication), new { id = model.ContainerId });
            }

            TempData["SuccessMessage"] = "Draft saved.";
            return RedirectToAction(nameof(Create_Publication), new { id = model.ContainerId });
        }

        // ---------- Profile ----------

        /// <summary>
        /// Read-only, and the photo is the exception rather than the first of several editable
        /// things. What is on this page — the student ID, the department, the programme, the cohort
        /// — is the institution's record of who this student is, and their work is filed and marked
        /// against it. Letting them retype it means a proposal can be assessed against a department
        /// nobody assigned. An administrator maintains it; the photo is theirs (ProfileController).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> studentprofile()
        {
            var model = await BuildProfileViewModelAsync();
            return model is null ? RedirectToAction(nameof(student_dashboard)) : View(model);
        }

        // ---------- Helpers ----------

        /// <summary>
        /// Resolves one of the student's own publications by id and, when a stage is given,
        /// refuses to open a stage that publication hasn't unlocked yet. The backend enforces
        /// both rules too; this keeps the student out of forms that would fail on submit, and
        /// stops one student's id from reaching another student's publication.
        /// </summary>
        private async Task<(PublicationContainerDto? Container, IActionResult? Redirect)> GetOwnedContainerAsync(
            Guid containerId, int? requiredStage = null)
        {
            var result = await containersApi.GetMineAsync(pageSize: 100);
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Could not load your publications right now.";
                return (null, RedirectToAction(nameof(student_dashboard)));
            }

            var container = (result.Data?.Items ?? []).FirstOrDefault(c => c.Id == containerId);
            if (container is null)
            {
                TempData["ErrorMessage"] = "That publication could not be found.";
                return (null, RedirectToAction(nameof(student_dashboard)));
            }

            if (requiredStage is { } stage && container.CurrentPipeline < stage)
            {
                TempData["ErrorMessage"] = stage switch
                {
                    PipelineStage.EthicsApproval =>
                        "The ethics stage opens once one of this publication's proposals has been approved and a supervisor assigned.",
                    PipelineStage.ResearchPaper =>
                        "The research paper stage opens once this publication's ethics approval has been completed.",
                    _ => "That stage isn't available yet."
                };

                return (null, RedirectToAction(nameof(Publication), new { id = containerId }));
            }

            return (container, null);
        }

        /// <summary>
        /// Surfaces an API failure on the form: per-field where the backend gave us field-level
        /// detail (FluentValidation), otherwise as a single summary error.
        /// </summary>
        private void AddApiErrors<T>(ApiResult<T> result, string fallbackMessage)
        {
            if (result.FieldErrors is { Count: > 0 })
            {
                foreach (var (field, messages) in result.FieldErrors)
                {
                    foreach (var message in messages) ModelState.AddModelError(field, message);
                }

                return;
            }

            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? fallbackMessage);
        }

        private async Task RepopulateGuidanceAsync(EthicsDeclarationPageViewModel model)
        {
            var guidanceResult = await ethicsApi.GetGuidanceAsync();
            model.Guidance = guidanceResult.Success ? guidanceResult.Data : null;
        }

        private async Task<StudentProfileViewModel?> BuildProfileViewModelAsync()
        {
            var result = await usersApi.GetMeAsync();
            if (!result.Success || result.Data is null) return null;

            var user = result.Data;
            var model = new StudentProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Status = user.Status,
                HasProfilePhoto = user.HasProfilePhoto
            };

            var studentProfile = user.Profile?.Deserialize<StudentProfileSummaryDto>(ProfileJsonOpts);
            if (studentProfile is not null)
            {
                model.StudentIdNumber = studentProfile.StudentIdNumber;
                model.DepartmentName = studentProfile.DepartmentName;
                model.Programme = studentProfile.Programme;
                model.Cohort = studentProfile.Cohort;
            }

            return model;
        }
    }
}
