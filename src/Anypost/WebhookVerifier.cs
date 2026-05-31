using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Anypost.Internal;
using Anypost.Models;

namespace Anypost;

/// <summary>Options for webhook signature verification.</summary>
public sealed class WebhookVerifyOptions
{
    /// <summary>
    /// The maximum delivery age, measured from its signed timestamp. Deliveries
    /// older than this are rejected to bound replay. Default is 300 seconds; a
    /// zero or negative value disables the freshness check.
    /// </summary>
    public TimeSpan Tolerance { get; init; } = WebhookVerifier.DefaultTolerance;

    /// <summary>Overrides the current time (Unix seconds) used for the freshness check. For tests.</summary>
    public long? Now { get; init; }
}

/// <summary>
/// Verifies the signature on an Anypost webhook delivery. Pass the raw request
/// body (the exact bytes received, before JSON parsing), the
/// <c>Anypost-Signature</c> header value, and the webhook's signing secret.
/// </summary>
public static class WebhookVerifier
{
    /// <summary>The default maximum age of a webhook delivery (300 seconds).</summary>
    public static readonly TimeSpan DefaultTolerance = TimeSpan.FromSeconds(300);

    /// <summary>
    /// Verifies the signature on a delivery. Returns normally on success and throws
    /// a <see cref="WebhookVerificationException"/> otherwise. The header may carry
    /// more than one <c>v1=</c> component during a secret rotation; a match on any
    /// one passes, so deliveries keep verifying across a rotation.
    /// </summary>
    public static void VerifySignature(
        byte[] payload,
        string signatureHeader,
        string secret,
        WebhookVerifyOptions? options = null)
    {
        options ??= new WebhookVerifyOptions();
        var (timestamp, signatures) = ParseSignatureHeader(signatureHeader);

        if (options.Tolerance > TimeSpan.Zero)
        {
            var now = options.Now ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var toleranceSeconds = (long)options.Tolerance.TotalSeconds;
            if (now - timestamp > toleranceSeconds)
            {
                throw new WebhookVerificationException(
                    WebhookVerificationReason.TimestampOutOfTolerance,
                    string.Format(CultureInfo.InvariantCulture, "Timestamp {0} is older than the {1}s tolerance.", timestamp, toleranceSeconds));
            }
        }

        var expected = ComputeSignature(timestamp, payload, secret);
        var expectedBytes = Encoding.ASCII.GetBytes(expected);

        // Constant-time over every candidate: accumulate without early exit.
        var matched = false;
        foreach (var candidate in signatures)
        {
            var candidateBytes = Encoding.ASCII.GetBytes(candidate);
            if (CryptographicOperations.FixedTimeEquals(candidateBytes, expectedBytes))
            {
                matched = true;
            }
        }

        if (!matched)
        {
            throw new WebhookVerificationException(
                WebhookVerificationReason.NoMatch,
                "No signature in the header matched the computed signature.");
        }
    }

    /// <summary>
    /// Verifies a delivery and returns its parsed body. A thin wrapper over
    /// <see cref="VerifySignature"/> that deserializes only after the signature
    /// checks out.
    /// </summary>
    public static WebhookDelivery Unwrap(
        byte[] payload,
        string signatureHeader,
        string secret,
        WebhookVerifyOptions? options = null)
    {
        VerifySignature(payload, signatureHeader, secret, options);

        try
        {
            var delivery = Json.Deserialize<WebhookDelivery>(payload);
            if (delivery is null)
            {
                throw new WebhookVerificationException(
                    WebhookVerificationReason.MalformedHeader,
                    "The webhook payload was empty.");
            }

            return delivery;
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new WebhookVerificationException(
                WebhookVerificationReason.MalformedHeader,
                "Could not decode the webhook payload: " + ex.Message);
        }
    }

    private static string ComputeSignature(long timestamp, byte[] payload, string secret)
    {
        var prefix = Encoding.ASCII.GetBytes(timestamp.ToString(CultureInfo.InvariantCulture) + ".");
        var message = new byte[prefix.Length + payload.Length];
        Buffer.BlockCopy(prefix, 0, message, 0, prefix.Length);
        Buffer.BlockCopy(payload, 0, message, prefix.Length, payload.Length);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(message);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static (long Timestamp, string[] Signatures) ParseSignatureHeader(string header)
    {
        if (string.IsNullOrEmpty(header))
        {
            throw new WebhookVerificationException(
                WebhookVerificationReason.MalformedHeader, "The Anypost-Signature header is empty.");
        }

        long? timestamp = null;
        var signatures = new System.Collections.Generic.List<string>();

        foreach (var part in header.Split(','))
        {
            var eq = part.IndexOf('=');
            if (eq < 0)
            {
                continue;
            }

            var key = part[..eq].Trim();
            var value = part[(eq + 1)..].Trim();
            switch (key)
            {
                case "t":
                    if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ts))
                    {
                        timestamp = ts;
                    }

                    break;
                case "v1":
                    signatures.Add(value);
                    break;
            }
        }

        if (timestamp is null)
        {
            throw new WebhookVerificationException(
                WebhookVerificationReason.NoTimestamp, "The Anypost-Signature header has no timestamp (t=).");
        }

        if (signatures.Count == 0)
        {
            throw new WebhookVerificationException(
                WebhookVerificationReason.NoSignatures, "The Anypost-Signature header has no v1= signature.");
        }

        return (timestamp.Value, signatures.ToArray());
    }
}
