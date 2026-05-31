using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Anypost.Internal;
using Anypost.Models;

namespace Anypost.Services;

/// <summary>
/// The <c>/suppressions</c> operations. Entries key on (email, topic). Access it
/// via <see cref="AnypostClient.Suppressions"/>.
/// </summary>
public sealed class SuppressionsService
{
    private readonly RequestExecutor _http;

    internal SuppressionsService(RequestExecutor http) => _http = http;

    /// <summary>Returns one page of the team's suppressions, newest-first. Expired rows are filtered out.</summary>
    public Task<Page<Suppression>> ListAsync(
        SuppressionListParams? listParams = null,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.ListAsync<Suppression>("/suppressions", listParams ?? new SuppressionListParams(), options, cancellationToken);

    /// <summary>
    /// Adds a manual suppression. Defaults to topic <c>*</c> (every topic). Returns
    /// a validation_error if an active entry for the same (email, topic) exists.
    /// </summary>
    public Task<Suppression> CreateAsync(
        SuppressionCreateParams request,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.SendAsync<Suppression>(HttpMethod.Post, "/suppressions", request, idempotent: false, query: null, options, cancellationToken);

    /// <summary>
    /// Retrieves the suppression for an (email, topic) pair. Use <c>*</c> as the
    /// topic for the global row. Throws a not_found error if the pair isn't suppressed.
    /// </summary>
    public Task<Suppression> GetAsync(
        string email,
        string topic,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.SendAsync<Suppression>(HttpMethod.Get, "/suppressions/" + PathUtil.Encode(email) + "/" + PathUtil.Encode(topic), body: null, idempotent: false, query: null, options, cancellationToken);

    /// <summary>Removes the single (email, topic) row. Other topics are untouched.</summary>
    public Task DeleteAsync(
        string email,
        string topic,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.SendNoContentAsync(HttpMethod.Delete, "/suppressions/" + PathUtil.Encode(email) + "/" + PathUtil.Encode(topic), body: null, query: null, options, cancellationToken);

    /// <summary>
    /// Returns every suppression on file for an address, across all topics. Throws a
    /// not_found error if the address has no active suppressions.
    /// </summary>
    public async Task<IReadOnlyList<Suppression>> ListForEmailAsync(
        string email,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var envelope = await _http.SendAsync<SuppressionListEnvelope>(
            HttpMethod.Get, "/suppressions/" + PathUtil.Encode(email), body: null, idempotent: false, query: null, options, cancellationToken)
            .ConfigureAwait(false);
        return envelope.Data;
    }

    /// <summary>Removes an address from the suppression list across every topic.</summary>
    public Task DeleteForEmailAsync(
        string email,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.SendNoContentAsync(HttpMethod.Delete, "/suppressions/" + PathUtil.Encode(email), body: null, query: null, options, cancellationToken);

    private sealed record SuppressionListEnvelope
    {
        public IReadOnlyList<Suppression> Data { get; init; } = new List<Suppression>();
    }
}
