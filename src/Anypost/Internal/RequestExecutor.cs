using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Anypost.Internal;

/// <summary>
/// Owns the HTTP transport and the request loop: header assembly, retries with
/// full-jitter backoff, idempotency keys, and error mapping. Shared by every
/// service.
/// </summary>
internal sealed class RequestExecutor
{
    private const int BaseBackoffMs = 500;
    private const int MaxBackoffMs = 8000;

    private static readonly HashSet<int> RetryableStatuses = new() { 429, 502, 503 };

    private static readonly string[] RequestIdHeaders =
    {
        "Anypost-Request-Id",
        "X-Anypost-Request-Id",
        "X-Request-Id",
    };

    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly int _maxRetries;
    private readonly TimeSpan _timeout;
    private readonly IReadOnlyDictionary<string, string> _defaultHeaders;
    private readonly string _userAgent;

    // Injectable for deterministic tests; default to Task.Delay and a real PRNG.
    internal Func<TimeSpan, CancellationToken, Task> Sleeper { get; set; } = (delay, ct) => Task.Delay(delay, ct);

    internal Func<double> Jitter { get; set; } = () => Random.Shared.NextDouble();

    internal RequestExecutor(
        HttpClient http,
        string apiKey,
        string baseUrl,
        int maxRetries,
        TimeSpan timeout,
        IReadOnlyDictionary<string, string> defaultHeaders,
        string userAgent)
    {
        _http = http;
        _apiKey = apiKey;
        _baseUrl = baseUrl.TrimEnd('/');
        _maxRetries = maxRetries;
        _timeout = timeout;
        _defaultHeaders = defaultHeaders;
        _userAgent = userAgent;
    }

    internal async Task<T> SendAsync<T>(
        HttpMethod method,
        string path,
        object? body,
        bool idempotent,
        Query? query,
        RequestOptions? options,
        CancellationToken cancellationToken)
    {
        var bytes = await SendCoreAsync(method, path, body, idempotent, query, options, cancellationToken)
            .ConfigureAwait(false);

        if (bytes.Length == 0)
        {
            return default!;
        }

        try
        {
            return Json.Deserialize<T>(bytes)!;
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw AnypostException.Connection("Could not decode the Anypost response.", ex);
        }
    }

    internal Task<Page<T>> ListAsync<T>(
        string path,
        ListParams listParams,
        RequestOptions? options,
        CancellationToken cancellationToken) =>
        FetchPageAsync<T>(path, listParams, after: null, options, cancellationToken);

    private async Task<Page<T>> FetchPageAsync<T>(
        string path,
        ListParams listParams,
        string? after,
        RequestOptions? options,
        CancellationToken cancellationToken)
    {
        var query = new Query();
        listParams.Apply(query);
        query.Add("after", after);

        var envelope = await SendAsync<PageEnvelope<T>>(
            HttpMethod.Get, path, body: null, idempotent: false, query, options, cancellationToken)
            .ConfigureAwait(false);

        return new Page<T>(
            envelope.Data,
            envelope.HasMore,
            envelope.NextCursor,
            (cursor, token) => FetchPageAsync<T>(path, listParams, cursor, options, token));
    }

    internal async Task SendNoContentAsync(
        HttpMethod method,
        string path,
        object? body,
        Query? query,
        RequestOptions? options,
        CancellationToken cancellationToken)
    {
        await SendCoreAsync(method, path, body, idempotent: false, query, options, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<byte[]> SendCoreAsync(
        HttpMethod method,
        string path,
        object? body,
        bool idempotent,
        Query? query,
        RequestOptions? options,
        CancellationToken cancellationToken)
    {
        byte[]? payload = body is null ? null : Json.SerializeToUtf8Bytes(body);
        var url = _baseUrl + path + (query?.Build() ?? string.Empty);

        // Build the idempotency key once so retries of a send reuse it.
        var idempotencyKey = ResolveIdempotencyKey(idempotent, options);

        for (var attempt = 0; ; attempt++)
        {
            HttpResponseMessage? response = null;
            try
            {
                using var request = BuildRequest(method, url, payload, idempotencyKey, options);
                response = await SendOnceAsync(request, cancellationToken).ConfigureAwait(false);
                var status = (int)response.StatusCode;
                var responseBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);

                if (status is >= 200 and < 300)
                {
                    return responseBytes;
                }

                if (RetryableStatuses.Contains(status) && attempt < _maxRetries)
                {
                    await BackoffAsync(attempt, response.Headers, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                throw AnypostException.FromResponse(
                    status,
                    responseBytes,
                    ReadRequestId(response.Headers),
                    ReadRetryAfter(response.Headers));
            }
            catch (HttpRequestException ex)
            {
                if (attempt < _maxRetries && !cancellationToken.IsCancellationRequested)
                {
                    await BackoffAsync(attempt, headers: null, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                throw AnypostException.Connection("Could not reach Anypost: " + ex.Message, ex);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // The caller did not cancel, so this is our per-request timeout.
                if (attempt < _maxRetries)
                {
                    await BackoffAsync(attempt, headers: null, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                throw AnypostException.Connection("Request timed out before a response.", ex);
            }
            finally
            {
                response?.Dispose();
            }
        }
    }

    private async Task<HttpResponseMessage> SendOnceAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_timeout <= TimeSpan.Zero)
        {
            return await _http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken)
                .ConfigureAwait(false);
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linked.CancelAfter(_timeout);
        return await _http.SendAsync(request, HttpCompletionOption.ResponseContentRead, linked.Token)
            .ConfigureAwait(false);
    }

    private string? ResolveIdempotencyKey(bool idempotent, RequestOptions? options)
    {
        if (!idempotent)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(options?.IdempotencyKey))
        {
            return options!.IdempotencyKey;
        }

        // Auto-key so built-in retries of a send cannot deliver twice.
        return _maxRetries > 0 ? Guid.NewGuid().ToString("D") : null;
    }

    private HttpRequestMessage BuildRequest(
        HttpMethod method,
        string url,
        byte[]? payload,
        string? idempotencyKey,
        RequestOptions? options)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _apiKey);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.Headers.TryAddWithoutValidation("User-Agent", _userAgent);

        foreach (var header in _defaultHeaders)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (idempotencyKey is not null)
        {
            request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        }

        if (options?.Headers is not null)
        {
            foreach (var header in options.Headers)
            {
                request.Headers.Remove(header.Key);
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        if (payload is not null)
        {
            var content = new ByteArrayContent(payload);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
            request.Content = content;
        }

        return request;
    }

    private async Task BackoffAsync(int attempt, HttpResponseHeaders? headers, CancellationToken cancellationToken)
    {
        var retryAfter = headers is null ? null : ReadRetryAfter(headers);
        if (retryAfter is { } after && after > TimeSpan.Zero)
        {
            var capped = after > TimeSpan.FromMilliseconds(MaxBackoffMs) ? TimeSpan.FromMilliseconds(MaxBackoffMs) : after;
            await Sleeper(capped, cancellationToken).ConfigureAwait(false);
            return;
        }

        var ceilingMs = Math.Min(MaxBackoffMs, BaseBackoffMs * Math.Pow(2, attempt));
        var delayMs = Jitter() * ceilingMs;
        await Sleeper(TimeSpan.FromMilliseconds(delayMs), cancellationToken).ConfigureAwait(false);
    }

    private static string? ReadRequestId(HttpResponseHeaders headers)
    {
        foreach (var name in RequestIdHeaders)
        {
            if (headers.TryGetValues(name, out var values))
            {
                foreach (var value in values)
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        return value;
                    }
                }
            }
        }

        return null;
    }

    private static TimeSpan? ReadRetryAfter(HttpResponseHeaders headers)
    {
        var retryAfter = headers.RetryAfter;
        if (retryAfter is null)
        {
            return null;
        }

        if (retryAfter.Delta is { } delta)
        {
            return delta > TimeSpan.Zero ? delta : TimeSpan.Zero;
        }

        if (retryAfter.Date is { } date)
        {
            var wait = date - DateTimeOffset.UtcNow;
            return wait > TimeSpan.Zero ? wait : TimeSpan.Zero;
        }

        return null;
    }
}
