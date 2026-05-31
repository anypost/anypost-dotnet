using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Anypost.Internal;
using Anypost.Models;

namespace Anypost.Services;

/// <summary>The <c>/api-keys</c> operations. Access it via <see cref="AnypostClient.ApiKeys"/>.</summary>
public sealed class ApiKeysService
{
    private readonly RequestExecutor _http;

    internal ApiKeysService(RequestExecutor http) => _http = http;

    /// <summary>Returns one page of the team's API keys, newest-first.</summary>
    public Task<Page<ApiKey>> ListAsync(
        ListParams? listParams = null,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.ListAsync<ApiKey>("/api-keys", listParams ?? new ListParams(), options, cancellationToken);

    /// <summary>
    /// Issues a new API key. The plaintext secret is returned only in this response,
    /// as <see cref="ApiKeyWithSecret.Key"/> — store it securely; it cannot be
    /// retrieved later.
    /// </summary>
    public Task<ApiKeyWithSecret> CreateAsync(
        ApiKeyCreateParams request,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.SendAsync<ApiKeyWithSecret>(HttpMethod.Post, "/api-keys", request, idempotent: false, query: null, options, cancellationToken);

    /// <summary>Retrieves a single API key's metadata. The secret is never returned.</summary>
    public Task<ApiKey> GetAsync(
        string id,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.SendAsync<ApiKey>(HttpMethod.Get, "/api-keys/" + PathUtil.Encode(id), body: null, idempotent: false, query: null, options, cancellationToken);

    /// <summary>
    /// Changes a key's name, permissions, and restrictions. The secret is not
    /// rotated here. Changes may take up to 5 minutes to propagate.
    /// </summary>
    public Task<ApiKey> UpdateAsync(
        string id,
        ApiKeyUpdateParams request,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.SendAsync<ApiKey>(HttpMethod.Patch, "/api-keys/" + PathUtil.Encode(id), request, idempotent: false, query: null, options, cancellationToken);

    /// <summary>Removes a key. It may keep authenticating for up to 5 minutes due to gateway caching.</summary>
    public Task DeleteAsync(
        string id,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.SendNoContentAsync(HttpMethod.Delete, "/api-keys/" + PathUtil.Encode(id), body: null, query: null, options, cancellationToken);
}
