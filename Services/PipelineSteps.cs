using Microsoft.Extensions.Caching.Memory;
using ResearchPublicationManagementSystem.Infrastructure.Api;

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
    Task<bool> HeadOfDepartmentReviewsEthicsAsync(CancellationToken ct = default);
}

/// <inheritdoc cref="IPipelineSteps"/>
public class PipelineSteps(SettingsApiClient settingsApi, IMemoryCache cache) : IPipelineSteps
{
    private const string CacheKey = "ethics-workflow";

    public async Task<bool> HeadOfDepartmentReviewsEthicsAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(CacheKey, out bool cached))
        {
            return cached;
        }

        var result = await settingsApi.GetEthicsWorkflowAsync(ct);

        // Nothing cached on a failure, and the answer is the sequence as it ships: a screen that
        // names a step which is switched off is a wording mistake, where one that omits a step
        // still in force hides a decision somebody is waiting to make.
        if (!result.Success || result.Data is null)
        {
            return true;
        }

        cache.Set(CacheKey, result.Data.HeadOfDepartmentReviews, TimeSpan.FromMinutes(1));
        return result.Data.HeadOfDepartmentReviews;
    }
}
