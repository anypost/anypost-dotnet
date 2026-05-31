using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Anypost;

namespace Anypost.Tests;

/// <summary>A captured outbound request, snapshotted before the executor disposes it.</summary>
internal sealed record CapturedRequest
{
    public required HttpMethod Method { get; init; }

    public required Uri RequestUri { get; init; }

    public required IReadOnlyDictionary<string, string> Headers { get; init; }

    public string? Body { get; init; }

    public string Header(string name) => Headers.TryGetValue(name, out var v) ? v : "";
}

/// <summary>
/// A test transport that records every request and returns queued responses. This
/// is the idiomatic <see cref="HttpClient"/> test seam — no bespoke interface.
/// </summary>
internal sealed class MockHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> _responders = new();

    internal List<CapturedRequest> Requests { get; } = new();

    internal void Enqueue(HttpStatusCode status, string json, Action<HttpResponseMessage>? configure = null)
    {
        _responders.Enqueue(_ =>
        {
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
            configure?.Invoke(response);
            return response;
        });
    }

    internal void EnqueueThrow(Exception exception) =>
        _responders.Enqueue(_ => throw exception);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in request.Headers)
        {
            headers[header.Key] = string.Join(",", header.Value);
        }

        string? body = null;
        if (request.Content is not null)
        {
            if (request.Content.Headers.ContentType is { } contentType)
            {
                headers["Content-Type"] = contentType.ToString();
            }

            body = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }

        Requests.Add(new CapturedRequest
        {
            Method = request.Method,
            RequestUri = request.RequestUri!,
            Headers = headers,
            Body = body,
        });

        if (_responders.Count == 0)
        {
            throw new InvalidOperationException("MockHandler received an unexpected request: " + request.RequestUri);
        }

        return _responders.Dequeue()(request);
    }
}

internal static class TestClient
{
    /// <summary>
    /// Builds a client wired to a fresh <see cref="MockHandler"/>, with deterministic
    /// backoff: sleeps are recorded (not awaited) and jitter is fixed at 1.0.
    /// </summary>
    internal static AnypostClient Create(out MockHandler handler, out List<TimeSpan> sleeps, int maxRetries = 2)
    {
        handler = new MockHandler();
        var recordedSleeps = new List<TimeSpan>();
        sleeps = recordedSleeps;

        var client = new AnypostClient("ap_test", new AnypostClientOptions
        {
            HttpClient = new HttpClient(handler),
            BaseUrl = "https://api.test/v1",
            MaxRetries = maxRetries,
            Timeout = TimeSpan.Zero,
        });

        client.Executor.Sleeper = (delay, _) =>
        {
            recordedSleeps.Add(delay);
            return Task.CompletedTask;
        };
        client.Executor.Jitter = () => 1.0;

        return client;
    }

    /// <summary>Computes the hex HMAC-SHA256 signature Anypost would send for a payload.</summary>
    internal static string Sign(long timestamp, string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var message = Encoding.UTF8.GetBytes(timestamp + "." + payload);
        return Convert.ToHexString(hmac.ComputeHash(message)).ToLowerInvariant();
    }
}
