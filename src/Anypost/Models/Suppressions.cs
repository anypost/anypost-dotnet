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

    /// <summary>When it stops applying; null means never.</summary>
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
