using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Anypost;
using Anypost.Models;
using Xunit;

namespace Anypost.Tests;

public class RetriesTests
{
    private static SendEmailRequest Email() =>
        new() { From = "a@b.com", To = ["c@d.com"], Text = "x", Subject = "s" };

    [Fact]
    public async Task Retries_on_429_then_succeeds()
    {
        using var client = TestClient.Create(out var handler, out var sleeps);
        handler.Enqueue(HttpStatusCode.TooManyRequests, """{"error":{"type":"rate_limit_exceeded","message":"slow"}}""");
        handler.Enqueue(HttpStatusCode.Accepted, """{"id":"email_1","created_at":"2026-01-01T00:00:00Z"}""");

        var sent = await client.Email.SendAsync(Email());

        Assert.Equal("email_1", sent.Id);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Single(sleeps);
        Assert.Equal(TimeSpan.FromMilliseconds(500), sleeps[0]); // base backoff, jitter fixed at 1.0
    }

    [Fact]
    public async Task Reuses_one_idempotency_key_across_send_retries()
    {
        using var client = TestClient.Create(out var handler, out _);
        handler.Enqueue(HttpStatusCode.ServiceUnavailable, """{"error":{"type":"provisioning_error","message":"x"}}""");
        handler.Enqueue(HttpStatusCode.Accepted, """{"id":"email_1","created_at":"2026-01-01T00:00:00Z"}""");

        await client.Email.SendAsync(Email());

        Assert.Equal(2, handler.Requests.Count);
        var first = handler.Requests[0].Header("Idempotency-Key");
        var second = handler.Requests[1].Header("Idempotency-Key");
        Assert.False(string.IsNullOrEmpty(first));
        Assert.Equal(first, second);
    }

    [Fact]
    public async Task Honors_retry_after_header()
    {
        using var client = TestClient.Create(out var handler, out var sleeps);
        handler.Enqueue(HttpStatusCode.TooManyRequests, """{"error":{"type":"rate_limit_exceeded","message":"slow"}}""",
            response => response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(2)));
        handler.Enqueue(HttpStatusCode.Accepted, """{"id":"email_1","created_at":"2026-01-01T00:00:00Z"}""");

        await client.Email.SendAsync(Email());

        Assert.Equal(TimeSpan.FromSeconds(2), Assert.Single(sleeps));
    }

    [Fact]
    public async Task Does_not_retry_a_500()
    {
        using var client = TestClient.Create(out var handler, out var sleeps);
        handler.Enqueue(HttpStatusCode.InternalServerError, """{"error":{"type":"internal_error","message":"boom"}}""");

        var ex = await Assert.ThrowsAsync<AnypostException>(() => client.Email.SendAsync(Email()));

        Assert.Equal(ErrorType.Internal, ex.Type);
        Assert.Single(handler.Requests);
        Assert.Empty(sleeps);
    }

    [Fact]
    public async Task Gives_up_after_maxRetries()
    {
        using var client = TestClient.Create(out var handler, out var sleeps, maxRetries: 2);
        for (var i = 0; i < 3; i++)
        {
            handler.Enqueue(HttpStatusCode.ServiceUnavailable, """{"error":{"type":"provisioning_error","message":"x"}}""");
        }

        var ex = await Assert.ThrowsAsync<AnypostException>(() => client.Email.SendAsync(Email()));

        Assert.Equal(ErrorType.Provisioning, ex.Type);
        Assert.Equal(3, handler.Requests.Count); // 1 initial + 2 retries
        Assert.Equal(2, sleeps.Count);
    }

    [Fact]
    public async Task Retries_a_network_error_then_succeeds()
    {
        using var client = TestClient.Create(out var handler, out _);
        handler.EnqueueThrow(new HttpRequestException("connection reset"));
        handler.Enqueue(HttpStatusCode.Accepted, """{"id":"email_1","created_at":"2026-01-01T00:00:00Z"}""");

        var sent = await client.Email.SendAsync(Email());

        Assert.Equal("email_1", sent.Id);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Surfaces_a_connection_error_after_exhausting_retries()
    {
        using var client = TestClient.Create(out var handler, out _, maxRetries: 1);
        handler.EnqueueThrow(new HttpRequestException("dns failure"));
        handler.EnqueueThrow(new HttpRequestException("dns failure"));

        var ex = await Assert.ThrowsAsync<AnypostException>(() => client.Email.SendAsync(Email()));

        Assert.Equal(ErrorType.Connection, ex.Type);
        Assert.Null(ex.StatusCode);
    }
}
