using System;
using System.Text;
using Anypost;
using Xunit;

namespace Anypost.Tests;

public class WebhookTests
{
    private const string Secret = "whsec_test_secret";
    private const string Payload = """{"batch_id":"wb_1","timestamp":1730000000,"events":[{"id":"ev_1","type":"email.delivered","occurred_at":"2026-01-01T00:00:00Z","data":{"email_id":"email_1"}}]}""";

    private static byte[] Body => Encoding.UTF8.GetBytes(Payload);

    private static WebhookVerifyOptions At(long timestamp) =>
        new() { Now = timestamp };

    [Fact]
    public void Verifies_a_valid_signature()
    {
        const long ts = 1730000000;
        var header = $"t={ts},v1={TestClient.Sign(ts, Payload, Secret)}";

        // Should not throw.
        WebhookVerifier.VerifySignature(Body, header, Secret, At(ts + 10));
    }

    [Fact]
    public void Unwrap_returns_the_parsed_delivery()
    {
        const long ts = 1730000000;
        var header = $"t={ts},v1={TestClient.Sign(ts, Payload, Secret)}";

        var delivery = WebhookVerifier.Unwrap(Body, header, Secret, At(ts + 10));

        Assert.Equal("wb_1", delivery.BatchId);
        Assert.Equal(1730000000, delivery.Timestamp);
        var ev = Assert.Single(delivery.Events);
        Assert.Equal("ev_1", ev.Id);
        Assert.Equal("email.delivered", ev.Type);
        Assert.Equal("email_1", ev.Data!["email_id"]?.ToString());
    }

    [Fact]
    public void Rejects_a_tampered_signature()
    {
        const long ts = 1730000000;
        var header = $"t={ts},v1=deadbeef";

        var ex = Assert.Throws<WebhookVerificationException>(() =>
            WebhookVerifier.VerifySignature(Body, header, Secret, At(ts + 10)));

        Assert.Equal(WebhookVerificationReason.NoMatch, ex.Reason);
    }

    [Fact]
    public void Accepts_any_matching_signature_during_rotation()
    {
        const long ts = 1730000000;
        var valid = TestClient.Sign(ts, Payload, Secret);
        var header = $"t={ts},v1=oldsecretsig,v1={valid}";

        WebhookVerifier.VerifySignature(Body, header, Secret, At(ts + 10));
    }

    [Fact]
    public void Rejects_a_stale_delivery()
    {
        const long ts = 1730000000;
        var header = $"t={ts},v1={TestClient.Sign(ts, Payload, Secret)}";

        var ex = Assert.Throws<WebhookVerificationException>(() =>
            WebhookVerifier.VerifySignature(Body, header, Secret, new WebhookVerifyOptions
            {
                Now = ts + 1000,
                Tolerance = TimeSpan.FromSeconds(300),
            }));

        Assert.Equal(WebhookVerificationReason.TimestampOutOfTolerance, ex.Reason);
    }

    [Fact]
    public void Rejects_a_header_with_no_timestamp()
    {
        var ex = Assert.Throws<WebhookVerificationException>(() =>
            WebhookVerifier.VerifySignature(Body, "v1=abc", Secret));

        Assert.Equal(WebhookVerificationReason.NoTimestamp, ex.Reason);
    }

    [Fact]
    public void Rejects_a_header_with_no_signature()
    {
        var ex = Assert.Throws<WebhookVerificationException>(() =>
            WebhookVerifier.VerifySignature(Body, "t=1730000000", Secret));

        Assert.Equal(WebhookVerificationReason.NoSignatures, ex.Reason);
    }

    [Fact]
    public void Rejects_an_empty_header()
    {
        var ex = Assert.Throws<WebhookVerificationException>(() =>
            WebhookVerifier.VerifySignature(Body, "", Secret));

        Assert.Equal(WebhookVerificationReason.MalformedHeader, ex.Reason);
    }
}
