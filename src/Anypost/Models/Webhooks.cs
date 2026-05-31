using System.Collections.Generic;

namespace Anypost.Models;

/// <summary>A webhook subscription. The signing secret is never returned here.</summary>
public record Webhook
{
    public string Id { get; init; } = "";

    public string Name { get; init; } = "";

    public string Url { get; init; } = "";

    public IReadOnlyList<WebhookEventType> Events { get; init; } = new List<WebhookEventType>();

    public WebhookStatus Status { get; init; }

    /// <summary>The first 12 characters of the signing secret.</summary>
    public string SigningSecretPrefix { get; init; } = "";

    /// <summary>The prefix of the previous secret while a rotation grace window is open, else null.</summary>
    public string? SigningSecretPreviousPrefix { get; init; }

    /// <summary>When the rotation grace window ends, or null.</summary>
    public string? SigningSecretGraceExpiresAt { get; init; }

    public string? LastDeliveryAt { get; init; }

    public string CreatedAt { get; init; } = "";
}

/// <summary>
/// A webhook with its full signing secret. Returned only on create and
/// rotate-secret.
/// </summary>
public sealed record WebhookWithSecret : Webhook
{
    /// <summary>The full signing secret (<c>whsec_...</c>). Returned once; store it securely.</summary>
    public string SigningSecret { get; init; } = "";
}

/// <summary>
/// The outcome of a synchronous test delivery. A bad endpoint never throws — read
/// <see cref="Delivered"/> and <see cref="StatusCode"/>.
/// </summary>
public sealed record WebhookTestResult
{
    /// <summary>True only when the endpoint returned a 2xx status.</summary>
    public bool Delivered { get; init; }

    /// <summary>The HTTP status the endpoint returned, or null on a network failure.</summary>
    public int? StatusCode { get; init; }

    /// <summary>Wall-clock time from request start to response or error.</summary>
    public int LatencyMs { get; init; }

    /// <summary>A human-readable failure reason, or null on success.</summary>
    public string? Error { get; init; }

    /// <summary>A truncated preview of the endpoint's response body.</summary>
    public string? ResponseBodyPreview { get; init; }
}

/// <summary>The body for creating a webhook.</summary>
public sealed record WebhookCreateParams
{
    public required string Name { get; init; }

    /// <summary>An <c>https://</c> endpoint to receive signed deliveries.</summary>
    public required string Url { get; init; }

    /// <summary>At least one event type to subscribe to.</summary>
    public required IReadOnlyList<WebhookEventType> Events { get; init; }
}

/// <summary>The body for updating a webhook.</summary>
public sealed record WebhookUpdateParams
{
    public required string Name { get; init; }

    public required string Url { get; init; }

    public required IReadOnlyList<WebhookEventType> Events { get; init; }

    /// <summary>Set <c>Disabled</c> to pause delivery or <c>Active</c> to resume.</summary>
    public required WebhookStatus Status { get; init; }
}

/// <summary>The outer envelope of a webhook delivery: one batch of one or more events.</summary>
public sealed record WebhookDelivery
{
    /// <summary>Identifies this batch. Stable across retries — de-duplicate on it.</summary>
    public string BatchId { get; init; } = "";

    /// <summary>The Unix timestamp the batch was signed with.</summary>
    public long Timestamp { get; init; }

    public IReadOnlyList<WebhookDeliveryEvent> Events { get; init; } = new List<WebhookDeliveryEvent>();
}

/// <summary>One event inside a <see cref="WebhookDelivery"/>.</summary>
public sealed record WebhookDeliveryEvent
{
    /// <summary>The unique event id. Stable across retries — de-duplicate on it.</summary>
    public string Id { get; init; } = "";

    /// <summary>A webhook event type (e.g. <c>email.delivered</c>) or <c>webhook.test</c>.</summary>
    public string Type { get; init; } = "";

    public string OccurredAt { get; init; } = "";

    /// <summary>Always carries <c>email_id</c>; the rest depends on the event type.</summary>
    public IReadOnlyDictionary<string, object?>? Data { get; init; }
}
