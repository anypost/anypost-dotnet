using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Anypost.Internal;
using Anypost.Models;

namespace Anypost.Services;

/// <summary>
/// The <c>/webhooks</c> operations, including test and rotation. Access it via
/// <see cref="AnypostClient.Webhooks"/>.
/// </summary>
public sealed class WebhooksService
{
    private readonly RequestExecutor _http;

    internal WebhooksService(RequestExecutor http) => _http = http;

    /// <summary>Returns one page of the team's webhooks, newest-first.</summary>
    public Task<Page<Webhook>> ListAsync(
        ListParams? listParams = null,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.ListAsync<Webhook>("/webhooks", listParams ?? new ListParams(), options, cancellationToken);

    /// <summary>
    /// Makes a webhook. The full signing secret is on this response only — store it
    /// now to verify future deliveries; later reads return only the prefix.
    /// </summary>
    public Task<WebhookWithSecret> CreateAsync(
        WebhookCreateParams request,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.SendAsync<WebhookWithSecret>(HttpMethod.Post, "/webhooks", request, idempotent: false, query: null, options, cancellationToken);

    /// <summary>Retrieves a webhook. The signing secret is never returned — only its prefix.</summary>
    public Task<Webhook> GetAsync(
        string id,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.SendAsync<Webhook>(HttpMethod.Get, "/webhooks/" + PathUtil.Encode(id), body: null, idempotent: false, query: null, options, cancellationToken);

    /// <summary>
    /// Changes a webhook's name, URL, events, and status. It does not rotate the
    /// signing secret — use <see cref="RotateSecretAsync"/>.
    /// </summary>
    public Task<Webhook> UpdateAsync(
        string id,
        WebhookUpdateParams request,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.SendAsync<Webhook>(HttpMethod.Patch, "/webhooks/" + PathUtil.Encode(id), request, idempotent: false, query: null, options, cancellationToken);

    /// <summary>Permanently removes a webhook.</summary>
    public Task DeleteAsync(
        string id,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.SendNoContentAsync(HttpMethod.Delete, "/webhooks/" + PathUtil.Encode(id), body: null, query: null, options, cancellationToken);

    /// <summary>
    /// Sends one synthetic <c>webhook.test</c> event and reports the outcome.
    /// One-shot, not retried, and absent from delivery history. Returns the result
    /// even when the endpoint fails — read <see cref="WebhookTestResult.Delivered"/>
    /// and <see cref="WebhookTestResult.StatusCode"/>. Works on a disabled webhook too.
    /// </summary>
    public Task<WebhookTestResult> TestAsync(
        string id,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.SendAsync<WebhookTestResult>(HttpMethod.Post, "/webhooks/" + PathUtil.Encode(id) + "/test", body: null, idempotent: false, query: null, options, cancellationToken);

    /// <summary>
    /// Rotates the signing secret. The new secret is on this response only. The
    /// previous secret stays valid for a 24h grace window. Rotating again before the
    /// window ends returns a <see cref="ErrorType.WebhookRotationInProgress"/> conflict.
    /// </summary>
    public Task<WebhookWithSecret> RotateSecretAsync(
        string id,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.SendAsync<WebhookWithSecret>(HttpMethod.Post, "/webhooks/" + PathUtil.Encode(id) + "/rotate-secret", body: null, idempotent: false, query: null, options, cancellationToken);
}
