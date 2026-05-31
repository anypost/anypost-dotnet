using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Anypost.Internal;
using Anypost.Models;

namespace Anypost.Services;

/// <summary>The <c>/domains</c> operations. Access it via <see cref="AnypostClient.Domains"/>.</summary>
public sealed class DomainsService
{
    private readonly RequestExecutor _http;

    internal DomainsService(RequestExecutor http) => _http = http;

    /// <summary>Returns one page of the team's domains, newest-first.</summary>
    public Task<Page<Domain>> ListAsync(
        ListParams? listParams = null,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.ListAsync<Domain>("/domains", listParams ?? new ListParams(), options, cancellationToken);

    /// <summary>Adds a sending domain. The returned domain is pending until verified.</summary>
    public Task<Domain> CreateAsync(
        DomainCreateParams request,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.SendAsync<Domain>(HttpMethod.Post, "/domains", request, idempotent: false, query: null, options, cancellationToken);

    /// <summary>Retrieves a single domain by id.</summary>
    public Task<Domain> GetAsync(
        string id,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.SendAsync<Domain>(HttpMethod.Get, "/domains/" + PathUtil.Encode(id), body: null, idempotent: false, query: null, options, cancellationToken);

    /// <summary>Changes a domain's tracking configuration. The domain name is immutable.</summary>
    public Task<Domain> UpdateAsync(
        string id,
        DomainUpdateParams request,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.SendAsync<Domain>(HttpMethod.Patch, "/domains/" + PathUtil.Encode(id), request, idempotent: false, query: null, options, cancellationToken);

    /// <summary>Permanently removes a domain and its DKIM keys.</summary>
    public Task DeleteAsync(
        string id,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.SendNoContentAsync(HttpMethod.Delete, "/domains/" + PathUtil.Encode(id), body: null, query: null, options, cancellationToken);

    /// <summary>
    /// Triggers a verification check. Always returns the current domain — read
    /// <see cref="Domain.Status"/> and <see cref="Domain.VerificationFailure"/> to
    /// learn the outcome. Safe to poll while DNS propagates.
    /// </summary>
    public Task<Domain> VerifyAsync(
        string id,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.SendAsync<Domain>(HttpMethod.Post, "/domains/" + PathUtil.Encode(id) + "/verify", body: null, idempotent: false, query: null, options, cancellationToken);
}
