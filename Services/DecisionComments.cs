using Microsoft.Extensions.Caching.Memory;
using ResearchPublicationManagementSystem.Infrastructure.Api;

namespace ResearchPublicationManagementSystem.Services;

/// <summary>
/// Whether a given decision has to carry a comment, as this institution has set it.
///
/// Every screen where a decision is made asks this before drawing its buttons, so that the field
/// is marked and the click is stopped here rather than after a round trip. The API enforces the
/// same policy; this is so the person is told before losing the page and what they typed.
/// </summary>
public interface IDecisionComments
{
    Task<bool> IsRequiredAsync(string decisionKey, CancellationToken ct = default);
}

/// <inheritdoc cref="IDecisionComments"/>
public class DecisionComments(SettingsApiClient settingsApi, IMemoryCache cache) : IDecisionComments
{
    private const string CacheKey = "decision-comments";

    public async Task<bool> IsRequiredAsync(string decisionKey, CancellationToken ct = default)
    {
        var policy = await LoadAsync(ct);

        // A key the API does not know is a mistake in the markup rather than a configuration, and
        // the safe answer to "must this be explained" is yes: it costs a sentence, where the other
        // way round costs a decision nobody can account for.
        return !policy.TryGetValue(decisionKey, out var required) || required;
    }

    /// <summary>
    /// The whole policy in one request, cached for a minute.
    ///
    /// A screen can carry a dozen decisions across its cards, so asking per button would be a
    /// dozen requests to draw one page. A minute matches how the rest of the site treats settings:
    /// an administrator's change shows up shortly rather than after a restart.
    /// </summary>
    private async Task<IReadOnlyDictionary<string, bool>> LoadAsync(CancellationToken ct)
    {
        if (cache.TryGetValue(CacheKey, out IReadOnlyDictionary<string, bool>? cached) && cached is not null)
        {
            return cached;
        }

        var result = await settingsApi.GetDecisionCommentsAsync(ct);

        // Nothing cached on a failure. An empty map reads as "everything is required", which is
        // the safe way to be wrong, and the next page load asks again rather than holding a wrong
        // answer for a minute.
        if (!result.Success || result.Data is null)
        {
            return new Dictionary<string, bool>();
        }

        var policy = result.Data.Decisions.ToDictionary(
            d => d.Key, d => d.CommentRequired, StringComparer.OrdinalIgnoreCase);

        cache.Set(CacheKey, (IReadOnlyDictionary<string, bool>)policy, TimeSpan.FromMinutes(1));
        return policy;
    }
}
