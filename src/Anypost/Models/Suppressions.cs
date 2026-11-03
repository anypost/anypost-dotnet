using System.Collections.Generic;
using Anypost.Internal;

namespace Anypost.Models;

/// <summary>A suppressed recipient address, scoped to a topic.</summary>
public sealed record Suppression
{
    /// <summary>The <c>sup_</c>-prefixed id, for log correlation. Lookups/deletes key on (email, topic).</summary>
    public string Id { get; init; } = "";

    /// <summary>The suppressed address, normalized to lowercase.</summary>
    public string Email { get; init; } = "";

    /// <summary>The topic this suppression applies to. <c>*</c> means every topic.</summary>
    public string Topic { get; init; } = "";

    public SuppressionReason Reason { get; init; }

    public SuppressionOrigin Origin { get; init; }

    /// <summary>A bounce classification or ARF feedback-type, null for manual entries.</summary>
    public string? Classification { get; init; }

    /// <summary>The SMTP reply code from the bounce, null for complaints and manual entries.</summary>
    public int? SmtpCode { get; init; }

    /// <summary>A free-form note attached at creation.</summary>
    public string? Note { get; init; }

    /// <summary>When the suppression was first observed.</summary>
    public string SuppressedAt { get; init; } = "";

    /// <summary>
    /// Always null: suppressions do not expire. Kept for compatibility and will be
    /// removed in a future major version.
    /// </summary>
    public string? ExpiresAt { get; init; }

    public string CreatedAt { get; init; } = "";
}

/// <summary>The filters for listing suppressions.</summary>
public sealed class SuppressionListParams : ListParams
{
    /// <summary>A case-insensitive substring match against the address.</summary>
    public string? EmailContains { get; init; }

    /// <summary>Restricts to a topic. <c>*</c> for global entries.</summary>
    public string? Topic { get; init; }

    public SuppressionReason? Reason { get; init; }

    public SuppressionOrigin? Origin { get; init; }

    internal override void Apply(Query query)
    {
        base.Apply(query);
        query.Add("email_contains", EmailContains);
        query.Add("topic", Topic);
        query.Add("reason", EnumWire.Value(Reason));
        query.Add("origin", EnumWire.Value(Origin));
    }
}

/// <summary>The body for creating a manual suppression.</summary>
public sealed record SuppressionCreateParams
{
    public required string Email { get; init; }

    /// <summary>Scopes the suppression. Omit or <c>*</c> to block every topic.</summary>
    public string? Topic { get; init; }

    /// <summary>An optional internal annotation, preserved across automatic re-suppressions.</summary>
    public string? Note { get; init; }
}

/// <summary>One entry in a batch of manual suppressions.</summary>
public sealed record SuppressionBatchItem
{
    public required string Email { get; init; }

    /// <summary>Scopes the suppression. Omit or <c>*</c> to block every topic.</summary>
    public string? Topic { get; init; }

    /// <summary>
    /// A free-form reason. The server maps <c>permanent_bounce</c>, <c>complaint</c>,
    /// <c>unsubscribed</c>, <c>manual</c>, and common equivalents such as <c>hard bounce</c>
    /// or <c>spam report</c> (case and punctuation ignored); anything else records <c>manual</c>.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>An optional internal annotation, up to 2000 characters.</summary>
    public string? Note { get; init; }
}

/// <summary>The wire body for a batch create.</summary>
internal sealed record SuppressionBatchRequest
{
    public IReadOnlyList<SuppressionBatchItem> Suppressions { get; init; } = new List<SuppressionBatchItem>();
}

/// <summary>The outcome of a batch create.</summary>
public sealed record SuppressionBatchResult
{
    /// <summary>Suppressions this call created.</summary>
    public int Imported { get; init; }

    /// <summary>Entries whose (email, topic) was already suppressed, including repeats within the batch. Left unchanged.</summary>
    public int AlreadySuppressed { get; init; }

    /// <summary>Entries whose email is not a valid address.</summary>
    public IReadOnlyList<SuppressionBatchInvalid> Invalid { get; init; } = new List<SuppressionBatchInvalid>();
}

/// <summary>An entry a batch create rejected as an invalid address.</summary>
public sealed record SuppressionBatchInvalid
{
    /// <summary>The entry's zero-based position in the request.</summary>
    public int Index { get; init; }

    /// <summary>The address as sent, or null if none was usable.</summary>
    public string? Email { get; init; }
}
