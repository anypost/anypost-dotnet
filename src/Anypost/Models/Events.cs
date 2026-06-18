using System.Collections.Generic;
using Anypost.Internal;

namespace Anypost.Models;

/// <summary>
/// Bot classification for a proxied open or click. Pure-noise machine traffic
/// (mailbox prefetchers, scanners) never becomes an event, so the only kind a
/// customer ever sees is <c>proxy</c> — a real open whose origin is anonymized by a
/// mailbox image proxy (Gmail, Yahoo, etc.).
/// </summary>
public sealed record EventBot
{
    /// <summary>The detected mailbox image proxy, e.g. <c>google</c>, <c>yahoo</c>, <c>bing</c>.</summary>
    public string Source { get; init; } = "";

    /// <summary>Always <c>proxy</c> on customer-visible events.</summary>
    public string Kind { get; init; } = "";
}

/// <summary>
/// Tracking metadata on <c>email.opened</c> / <c>email.clicked</c> events, mirroring the
/// webhook payload's <c>data.tracking</c>. <see cref="Bot"/> is set only when the
/// interaction came from a mailbox image proxy; a human open/click has no bot.
/// </summary>
public sealed record EventTracking
{
    public EventBot? Bot { get; init; }
}

/// <summary>
/// A single email-pipeline event for the team. Fields that don't apply to a given
/// event type are null.
/// </summary>
public sealed record Event
{
    /// <summary>The stable id for log correlation. Not addressable — there is no GET /events/{id}.</summary>
    public string Id { get; init; } = "";

    public EventType Type { get; init; }

    /// <summary>The ISO 8601 UTC timestamp when the event was observed.</summary>
    public string OccurredAt { get; init; } = "";

    /// <summary>The <c>email_&lt;uuidv7&gt;</c> id minted when the message was accepted.</summary>
    public string? EmailId { get; init; }

    /// <summary>The RFC 5322 <c>Message-ID:</c> header, when one was stamped.</summary>
    public string? MessageId { get; init; }

    /// <summary>The envelope <c>From:</c> address.</summary>
    public string? From { get; init; }

    /// <summary>The <c>From:</c> domain, lowercased.</summary>
    public string? FromDomain { get; init; }

    /// <summary>The single recipient this event refers to.</summary>
    public string? Recipient { get; init; }

    /// <summary>The captured <c>Subject:</c> header, truncated at the capture limit.</summary>
    public string? Subject { get; init; }

    public string? Campaign { get; init; }

    /// <summary>The public id of the template the originating send used.</summary>
    public string? TemplateId { get; init; }

    public string? Topic { get; init; }

    /// <summary>The customer-supplied tags from the originating send.</summary>
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>The SMTP reply code observed, or null without an SMTP exchange.</summary>
    public int? SmtpCode { get; init; }

    /// <summary>The bounce type (e.g. Hard, Soft). Only on <c>email.bounced</c>.</summary>
    public string? BounceType { get; init; }

    /// <summary>The bounce classification. Only on <c>email.bounced</c>.</summary>
    public string? BounceClassification { get; init; }

    /// <summary>The delivery attempt number, or null for non-delivery events.</summary>
    public int? Attempt { get; init; }

    /// <summary>
    /// Tracking metadata, mirroring the webhook payload's <c>data.tracking</c>. Null on
    /// every event except opens/clicks, and on human opens/clicks. Its
    /// <see cref="EventTracking.Bot"/> is set when the open/click came from a mailbox
    /// image proxy.
    /// </summary>
    public EventTracking? Tracking { get; init; }
}

/// <summary>
/// The filters for listing events. The window defaults to the last 24 hours and is
/// clamped to the plan's retention. All filters are exact-match except
/// <see cref="Tags"/> (has-any).
/// </summary>
public sealed class EventListParams : ListParams
{
    /// <summary>The ISO 8601 start of the window (inclusive).</summary>
    public string? Start { get; init; }

    /// <summary>The ISO 8601 end of the window (exclusive).</summary>
    public string? End { get; init; }

    public EventType? EventType { get; init; }

    /// <summary>An exact recipient address.</summary>
    public string? Recipient { get; init; }

    /// <summary>Restricts to one message's <c>email_&lt;uuidv7&gt;</c> id.</summary>
    public string? EmailId { get; init; }

    /// <summary>An exact <c>Message-ID:</c> header match.</summary>
    public string? MessageId { get; init; }

    /// <summary>A sending-domain hostname (not the <c>domain_&lt;uuid&gt;</c> id).</summary>
    public string? Domain { get; init; }

    public string? Topic { get; init; }

    /// <summary>A case-sensitive exact match.</summary>
    public string? Campaign { get; init; }

    /// <summary>The template the originating send used.</summary>
    public string? TemplateId { get; init; }

    /// <summary>Restricts to events carrying any of these tags (has-any). Up to 10.</summary>
    public IReadOnlyList<string>? Tags { get; init; }

    internal override void Apply(Query query)
    {
        base.Apply(query);
        query.Add("start", Start);
        query.Add("end", End);
        query.Add("event_type", EnumWire.Value(EventType));
        query.Add("recipient", Recipient);
        query.Add("email_id", EmailId);
        query.Add("message_id", MessageId);
        query.Add("domain", Domain);
        query.Add("topic", Topic);
        query.Add("campaign", Campaign);
        query.Add("template_id", TemplateId);
        if (Tags is { Count: > 0 })
        {
            query.Add("tags", string.Join(",", Tags));
        }
    }
}
