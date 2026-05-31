using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Anypost.Internal;
using Anypost.Models;

namespace Anypost.Services;

/// <summary>
/// The <c>/templates</c> operations, including the draft/publish flow. Access it
/// via <see cref="AnypostClient.Templates"/>.
/// </summary>
public sealed class TemplatesService
{
    private readonly RequestExecutor _http;

    internal TemplatesService(RequestExecutor http) => _http = http;

    /// <summary>Returns one page of the team's templates, newest-first.</summary>
    public Task<Page<Template>> ListAsync(
        ListParams? listParams = null,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.ListAsync<Template>("/templates", listParams ?? new ListParams(), options, cancellationToken);

    /// <summary>Creates a template. It starts unpublished — publish it before sending.</summary>
    public Task<Template> CreateAsync(
        TemplateCreateParams request,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.SendAsync<Template>(HttpMethod.Post, "/templates", request, idempotent: false, query: null, options, cancellationToken);

    /// <summary>Retrieves a template, including its published content.</summary>
    public Task<Template> GetAsync(
        string id,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.SendAsync<Template>(HttpMethod.Get, "/templates/" + PathUtil.Encode(id), body: null, idempotent: false, query: null, options, cancellationToken);

    /// <summary>Changes a template's name. Body content lives on the draft.</summary>
    public Task<Template> UpdateAsync(
        string id,
        TemplateUpdateParams request,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.SendAsync<Template>(HttpMethod.Patch, "/templates/" + PathUtil.Encode(id), request, idempotent: false, query: null, options, cancellationToken);

    /// <summary>Permanently removes a template.</summary>
    public Task DeleteAsync(
        string id,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.SendNoContentAsync(HttpMethod.Delete, "/templates/" + PathUtil.Encode(id), body: null, query: null, options, cancellationToken);

    /// <summary>
    /// Copies a template. The copy starts unpublished with a draft seeded from the
    /// source's current editable content. Pass null params to accept the default name.
    /// </summary>
    public Task<Template> DuplicateAsync(
        string id,
        TemplateDuplicateParams? request = null,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.SendAsync<Template>(HttpMethod.Post, "/templates/" + PathUtil.Encode(id) + "/duplicate", request, idempotent: false, query: null, options, cancellationToken);

    /// <summary>Retrieves the template's unpublished draft. Throws a not_found error if none exists.</summary>
    public Task<TemplateDraft> GetDraftAsync(
        string id,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.SendAsync<TemplateDraft>(HttpMethod.Get, "/templates/" + PathUtil.Encode(id) + "/draft", body: null, idempotent: false, query: null, options, cancellationToken);

    /// <summary>Creates or updates the template's draft. Idempotent upsert; published content is untouched.</summary>
    public Task<TemplateDraft> UpdateDraftAsync(
        string id,
        TemplateDraftParams request,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.SendAsync<TemplateDraft>(HttpMethod.Patch, "/templates/" + PathUtil.Encode(id) + "/draft", request, idempotent: false, query: null, options, cancellationToken);

    /// <summary>Discards the template's draft without touching published content.</summary>
    public Task DeleteDraftAsync(
        string id,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.SendNoContentAsync(HttpMethod.Delete, "/templates/" + PathUtil.Encode(id) + "/draft", body: null, query: null, options, cancellationToken);

    /// <summary>Promotes the draft into the published slot, consuming the draft.</summary>
    public Task<Template> PublishAsync(
        string id,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.SendAsync<Template>(HttpMethod.Post, "/templates/" + PathUtil.Encode(id) + "/publish", body: null, idempotent: false, query: null, options, cancellationToken);
}
