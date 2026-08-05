using Microsoft.Extensions.Caching.Memory;
using ResearchPublicationManagementSystem.Infrastructure.Api;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Services;

/// <summary>
/// Which of the optional steps this institution runs.
///
/// Screens ask this so that what they say matches the sequence actually in force: a coordinator
/// closing the ethics stage is told the Head of Department has commented only where there is a
/// Head of Department step to have commented.
/// </summary>
public interface IPipelineSteps
{
    /// <summary>Whether the Head of Department reads documentation the coordinator has approved.</summary>
    Task<bool> HeadOfDepartmentReviewsEthicsAsync(CancellationToken ct = default);

    /// <summary>The same, where the ruling was that no documentation is needed at all.</summary>
    Task<bool> HeadOfDepartmentReviewsWhenNotRequiredAsync(CancellationToken ct = default);
}

/// <inheritdoc cref="IPipelineSteps"/>
public class PipelineSteps(SettingsApiClient settingsApi, IMemoryCache cache) : IPipelineSteps
{
    private const string CacheKey = "ethics-workflow";

    public async Task<bool> HeadOfDepartmentReviewsEthicsAsync(CancellationToken ct = default) =>
        (await LoadAsync(ct)).HeadOfDepartmentReviews;

    public async Task<bool> HeadOfDepartmentReviewsWhenNotRequiredAsync(CancellationToken ct = default) =>
        (await LoadAsync(ct)).HeadOfDepartmentReviewsWhenNotRequired;

    private async Task<EthicsWorkflowSettingsDto> LoadAsync(CancellationToken ct)
    {
        if (cache.TryGetValue(CacheKey, out EthicsWorkflowSettingsDto? cached) && cached is not null)
        {
            return cached;
        }

        var result = await settingsApi.GetEthicsWorkflowAsync(ct);

        // Nothing cached on a failure, and the answer is the sequence as it ships: a screen that
        // names a step which is switched off is a wording mistake, where one that omits a step
        // still in force hides a decision somebody is waiting to make.
        if (!result.Success || result.Data is null)
        {
            return new EthicsWorkflowSettingsDto(true, true);
        }

        cache.Set(CacheKey, result.Data, TimeSpan.FromMinutes(1));
        return result.Data;
    }
}
