using System;

namespace Anypost;

/// <summary>The machine-readable cause of a webhook signature verification failure.</summary>
public enum WebhookVerificationReason
{
    /// <summary>The <c>Anypost-Signature</c> header could not be parsed.</summary>
    MalformedHeader,

    /// <summary>The header carried no <c>t=</c> component.</summary>
    NoTimestamp,

    /// <summary>The header carried no <c>v1=</c> component.</summary>
    NoSignatures,

    /// <summary>The delivery is older than the tolerance.</summary>
    TimestampOutOfTolerance,

    /// <summary>No <c>v1=</c> component matched the computed signature.</summary>
    NoMatch,
}

/// <summary>Thrown when a webhook delivery's signature cannot be verified.</summary>
public sealed class WebhookVerificationException : Exception
{
    internal WebhookVerificationException(WebhookVerificationReason reason, string message)
        : base(message)
    {
        Reason = reason;
    }

    /// <summary>The machine-readable cause. Branch on this rather than the message.</summary>
    public WebhookVerificationReason Reason { get; }
}
