using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Anypost.Internal;
using Anypost.Models;

namespace Anypost.Services;

/// <summary>The <c>/email</c> operations. Access it via <see cref="AnypostClient.Email"/>.</summary>
public sealed class EmailService
{
    private readonly RequestExecutor _http;

    internal EmailService(RequestExecutor http) => _http = http;

    /// <summary>
    /// Sends a single message. All addresses in to/cc/bcc share one envelope.
    /// When retries are enabled and no idempotency key is supplied, the client
    /// generates one so a retried send cannot deliver twice.
    /// </summary>
    public Task<SendResponse> SendAsync(
        SendEmailRequest request,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.SendAsync<SendResponse>(HttpMethod.Post, "/email", request, idempotent: true, query: null, options, cancellationToken);

    /// <summary>
    /// Sends 1 to 100 independent messages in one request. A mixed-outcome batch
    /// (HTTP 207) returns normally — inspect each entry's
    /// <see cref="BatchItemResult.Status"/>; it does not throw.
    /// </summary>
    public Task<BatchResponse> SendBatchAsync(
        EmailBatchRequest batch,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.SendAsync<BatchResponse>(HttpMethod.Post, "/email/batch", batch, idempotent: true, query: null, options, cancellationToken);
}
