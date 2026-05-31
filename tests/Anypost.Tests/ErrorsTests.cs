using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Anypost;
using Anypost.Models;
using Xunit;

namespace Anypost.Tests;

public class ErrorsTests
{
    private static async Task<AnypostException> CaptureSendError(MockHandler handler, AnypostClient client)
    {
        return await Assert.ThrowsAsync<AnypostException>(() =>
            client.Email.SendAsync(new SendEmailRequest { From = "a@b.com", To = ["c@d.com"], Text = "x", Subject = "s" }));
    }

    [Fact]
    public async Task Validation_error_exposes_field_problems()
    {
        using var client = TestClient.Create(out var handler, out _);
        handler.Enqueue(HttpStatusCode.UnprocessableEntity, """
        {"error":{"type":"validation_error","message":"The from field is required.","errors":{"from":["The from field is required."]}}}
        """);

        var ex = await CaptureSendError(handler, client);

        Assert.Equal(ErrorType.Validation, ex.Type);
        Assert.Equal(422, ex.StatusCode);
        Assert.NotNull(ex.ValidationErrors);
        Assert.Equal("The from field is required.", ex.ValidationErrors!["from"][0]);
    }

    [Fact]
    public async Task Authentication_error_maps_by_type()
    {
        using var client = TestClient.Create(out var handler, out _);
        handler.Enqueue(HttpStatusCode.Unauthorized, """{"error":{"type":"authentication_error","message":"bad key"}}""");

        var ex = await CaptureSendError(handler, client);

        Assert.Equal(ErrorType.Authentication, ex.Type);
        Assert.Equal(401, ex.StatusCode);
    }

    [Fact]
    public async Task Not_found_maps_by_type()
    {
        using var client = TestClient.Create(out var handler, out _);
        handler.Enqueue(HttpStatusCode.NotFound, """{"error":{"type":"not_found","message":"no such domain"}}""");

        var ex = await Assert.ThrowsAsync<AnypostException>(() => client.Domains.GetAsync("domain_x"));

        Assert.Equal(ErrorType.NotFound, ex.Type);
    }

    [Fact]
    public async Task Flat_413_envelope_maps_to_payload_too_large()
    {
        using var client = TestClient.Create(out var handler, out _);
        handler.Enqueue(HttpStatusCode.RequestEntityTooLarge, """{"error":"payload_too_large"}""");

        var ex = await CaptureSendError(handler, client);

        Assert.Equal(ErrorType.PayloadTooLarge, ex.Type);
        Assert.Equal(413, ex.StatusCode);
    }

    [Fact]
    public async Task Captures_the_request_id_header()
    {
        using var client = TestClient.Create(out var handler, out _);
        handler.Enqueue(HttpStatusCode.NotFound, """{"error":{"type":"not_found","message":"nope"}}""",
            response => response.Headers.TryAddWithoutValidation("Anypost-Request-Id", "req_abc"));

        var ex = await Assert.ThrowsAsync<AnypostException>(() => client.Domains.GetAsync("domain_x"));

        Assert.Equal("req_abc", ex.RequestId);
    }

    [Fact]
    public async Task Rate_limit_error_parses_retry_after()
    {
        // maxRetries 0 so the 429 surfaces as an error rather than being retried.
        using var client = TestClient.Create(out var handler, out _, maxRetries: 0);
        handler.Enqueue(HttpStatusCode.TooManyRequests, """{"error":{"type":"rate_limit_exceeded","message":"slow down"}}""",
            response => response.Headers.RetryAfter = new RetryConditionHeaderValue(System.TimeSpan.FromSeconds(7)));

        var ex = await CaptureSendError(handler, client);

        Assert.Equal(ErrorType.RateLimit, ex.Type);
        Assert.Equal(System.TimeSpan.FromSeconds(7), ex.RetryAfter);
    }
}
