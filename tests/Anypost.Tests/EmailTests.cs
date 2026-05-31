using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Anypost;
using Anypost.Models;
using Xunit;

namespace Anypost.Tests;

public class EmailTests
{
    [Fact]
    public async Task Send_posts_the_expected_body_and_parses_the_response()
    {
        using var client = TestClient.Create(out var handler, out _);
        handler.Enqueue(HttpStatusCode.Accepted, """{"id":"email_018f","created_at":"2026-04-30T12:00:00.123000Z"}""");

        var sent = await client.Email.SendAsync(new SendEmailRequest
        {
            From = "Acme <hello@example.com>",
            To = ["alex@customer.com"],
            ReplyTo = ["support@example.com"],
            Subject = "Welcome to Acme",
            Html = "<p>Glad you are here.</p>",
            Text = "Glad you are here.",
        });

        Assert.Equal("email_018f", sent.Id);
        Assert.Equal("2026-04-30T12:00:00.123000Z", sent.CreatedAt);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("POST", request.Method.Method);
        Assert.Equal("https://api.test/v1/email", request.RequestUri.ToString());

        using var body = JsonDocument.Parse(request.Body!);
        var root = body.RootElement;
        Assert.Equal("Acme <hello@example.com>", root.GetProperty("from").GetString());
        Assert.Equal("alex@customer.com", root.GetProperty("to")[0].GetString());
        Assert.Equal("support@example.com", root.GetProperty("reply_to")[0].GetString());
        Assert.Equal("Welcome to Acme", root.GetProperty("subject").GetString());
        Assert.Equal("<p>Glad you are here.</p>", root.GetProperty("html").GetString());
        // Null optional fields are omitted.
        Assert.False(root.TryGetProperty("cc", out _));
    }

    [Fact]
    public async Task Send_attaches_an_auto_idempotency_key()
    {
        using var client = TestClient.Create(out var handler, out _);
        handler.Enqueue(HttpStatusCode.Accepted, """{"id":"email_1","created_at":"2026-01-01T00:00:00Z"}""");

        await client.Email.SendAsync(new SendEmailRequest { From = "a@b.com", To = ["c@d.com"], Text = "hi", Subject = "s" });

        var key = Assert.Single(handler.Requests).Header("Idempotency-Key");
        Assert.True(Guid.TryParse(key, out _), "auto idempotency key should be a UUID");
    }

    [Fact]
    public async Task Send_uses_an_explicit_idempotency_key()
    {
        using var client = TestClient.Create(out var handler, out _);
        handler.Enqueue(HttpStatusCode.Accepted, """{"id":"email_1","created_at":"2026-01-01T00:00:00Z"}""");

        await client.Email.SendAsync(
            new SendEmailRequest { From = "a@b.com", To = ["c@d.com"], Text = "hi", Subject = "s" },
            new RequestOptions { IdempotencyKey = "my-key-123" });

        Assert.Equal("my-key-123", Assert.Single(handler.Requests).Header("Idempotency-Key"));
    }

    [Fact]
    public async Task SendBatch_does_not_throw_on_207_and_exposes_per_entry_outcomes()
    {
        using var client = TestClient.Create(out var handler, out _);
        handler.Enqueue((HttpStatusCode)207, """
        {
          "summary": {"total": 2, "queued": 1, "failed": 1},
          "data": [
            {"status": "queued", "index": 0, "id": "email_1", "created_at": "2026-01-01T00:00:00Z"},
            {"status": "failed", "index": 1, "error": {"type": "validation_error", "message": "bad to"}}
          ]
        }
        """);

        var batch = await client.Email.SendBatchAsync(new EmailBatchRequest
        {
            Emails = [
                new SendEmailRequest { From = "a@b.com", To = ["c@d.com"], Text = "x", Subject = "s" },
                new SendEmailRequest { From = "a@b.com", To = ["bad"], Text = "x", Subject = "s" },
            ],
        });

        Assert.Equal(2, batch.Summary.Total);
        Assert.Equal(1, batch.Summary.Queued);
        Assert.Equal(1, batch.Summary.Failed);

        Assert.True(batch.Data[0].IsQueued);
        Assert.Equal("email_1", batch.Data[0].Id);

        Assert.False(batch.Data[1].IsQueued);
        Assert.Equal("validation_error", batch.Data[1].Error!.Type);
    }

    [Fact]
    public async Task Attachments_are_base64_encoded_on_the_wire()
    {
        using var client = TestClient.Create(out var handler, out _);
        handler.Enqueue(HttpStatusCode.Accepted, """{"id":"email_1","created_at":"2026-01-01T00:00:00Z"}""");

        await client.Email.SendAsync(new SendEmailRequest
        {
            From = "a@b.com",
            To = ["c@d.com"],
            Subject = "s",
            Text = "hi",
            Attachments = [Attachment.Create("hello.txt", "hello"u8.ToArray(), "text/plain")],
        });

        using var body = JsonDocument.Parse(Assert.Single(handler.Requests).Body!);
        var attachment = body.RootElement.GetProperty("attachments")[0];
        Assert.Equal("hello.txt", attachment.GetProperty("filename").GetString());
        Assert.Equal(Convert.ToBase64String("hello"u8.ToArray()), attachment.GetProperty("content").GetString());
        Assert.Equal("text/plain", attachment.GetProperty("content_type").GetString());
    }
}
