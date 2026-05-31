using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Anypost.Models;

/// <summary>
/// A single message to send. For a standalone send, <see cref="From"/> and
/// <see cref="To"/> are required, and at least one of <see cref="Text"/>,
/// <see cref="Html"/>, or <see cref="TemplateId"/> must be set. As a batch entry,
/// any shared field may be omitted to inherit the batch defaults.
/// </summary>
public sealed record SendEmailRequest
{
    /// <summary>The sender address on a verified domain, bare or <c>Display Name &lt;addr@host&gt;</c>.</summary>
    public string? From { get; init; }

    /// <summary>1 to 50 primary recipients. Combined to + cc + bcc must be 50 or fewer.</summary>
    public IReadOnlyList<string>? To { get; init; }

    public IReadOnlyList<string>? Cc { get; init; }

    /// <summary>Blind-copied recipients. Counts against the combined recipient cap.</summary>
    public IReadOnlyList<string>? Bcc { get; init; }

    /// <summary>One address or up to 10 that replies are directed to.</summary>
    public IReadOnlyList<string>? ReplyTo { get; init; }

    /// <summary>The subject line. Required unless a referenced template supplies it.</summary>
    public string? Subject { get; init; }

    public string? Text { get; init; }

    public string? Html { get; init; }

    /// <summary>References a published template (<c>template_&lt;uuid&gt;</c>). Cannot be combined with inline text/html.</summary>
    public string? TemplateId { get; init; }

    /// <summary>Custom message headers. At most 25 survive server-side.</summary>
    public IReadOnlyDictionary<string, string>? Headers { get; init; }

    /// <summary>Up to 20 attachments.</summary>
    public IReadOnlyList<Attachment>? Attachments { get; init; }

    /// <summary>Up to 10 free-form labels (<c>[A-Za-z0-9_-]{1,64}</c>).</summary>
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>A stream-segmentation label (<c>[A-Za-z0-9_-]{1,64}</c>).</summary>
    public string? Campaign { get; init; }

    /// <summary>The suppression scope / topic bucket (<c>[a-z0-9_.-]{1,64}</c>).</summary>
    public string? Topic { get; init; }

    /// <summary>Overrides the domain's open/click defaults for this message.</summary>
    public Tracking? Tracking { get; init; }

    /// <summary>The Handlebars substitution map. Encoded JSON must be 64 KB or smaller.</summary>
    public IReadOnlyDictionary<string, object?>? Variables { get; init; }

    /// <summary>Controls one-click unsubscribe header injection.</summary>
    public Unsubscribe? Unsubscribe { get; init; }
}

/// <summary>The body for a batch send: 1 to 100 messages, with optional batch-wide defaults.</summary>
public sealed record EmailBatchRequest
{
    /// <summary>
    /// Fills any field an entry omits. Recipients are always per-entry, so leave
    /// <see cref="SendEmailRequest.To"/> unset here.
    /// </summary>
    public SendEmailRequest? Defaults { get; init; }

    /// <summary>The 1 to 100 messages in the batch.</summary>
    public required IReadOnlyList<SendEmailRequest> Emails { get; init; }
}

/// <summary>Returned by a successful single send.</summary>
public sealed record SendResponse
{
    /// <summary>The public message identifier (<c>email_&lt;uuidv7&gt;</c>).</summary>
    public string Id { get; init; } = "";

    public string CreatedAt { get; init; } = "";
}

/// <summary>Tallies a batch's per-entry outcomes.</summary>
public sealed record BatchSummary
{
    public int Total { get; init; }

    public int Queued { get; init; }

    public int Failed { get; init; }
}

/// <summary>The inner error on a failed batch entry.</summary>
public sealed record BatchItemError
{
    public string Type { get; init; } = "";

    public string Message { get; init; } = "";
}

/// <summary>
/// One entry's outcome in a batch send. Discriminate on <see cref="Status"/>:
/// queued entries carry <see cref="Id"/> and <see cref="CreatedAt"/>; failed
/// entries carry <see cref="Error"/>.
/// </summary>
public sealed record BatchItemResult
{
    public string Status { get; init; } = "";

    /// <summary>The zero-based position in the request's <c>emails</c> list.</summary>
    public int Index { get; init; }

    public string? Id { get; init; }

    public string? CreatedAt { get; init; }

    public BatchItemError? Error { get; init; }

    /// <summary>True when this entry was queued (as opposed to failed).</summary>
    [JsonIgnore]
    public bool IsQueued => Status == "queued";
}

/// <summary>
/// Returned from a batch send. A mixed-outcome batch (HTTP 207) is a success, not
/// an error: inspect each entry's <see cref="BatchItemResult.Status"/>.
/// </summary>
public sealed record BatchResponse
{
    public BatchSummary Summary { get; init; } = new();

    public IReadOnlyList<BatchItemResult> Data { get; init; } = new List<BatchItemResult>();
}
