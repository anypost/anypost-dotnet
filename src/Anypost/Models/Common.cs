using System;

namespace Anypost.Models;

/// <summary>
/// One attachment on a message. Supply the content exactly one of two ways:
/// <see cref="Content"/> for raw file bytes, which the SDK base64-encodes on the
/// wire, or <see cref="ContentBase64"/> when the data is already base64 at rest —
/// that string is sent through untouched, so nothing is decoded just to be
/// re-encoded.
/// </summary>
public sealed record Attachment
{
    /// <summary>The file name shown to the recipient.</summary>
    public required string Filename { get; init; }

    /// <summary>
    /// The raw file bytes; encoded to base64 on the wire. Leave null when
    /// setting <see cref="ContentBase64"/> instead.
    /// </summary>
    public byte[]? Content { get; init; }

    /// <summary>
    /// Already-base64-encoded file content, sent verbatim. Leave null when
    /// setting <see cref="Content"/> instead.
    /// </summary>
    public string? ContentBase64 { get; init; }

    /// <summary>The MIME type. Defaults to <c>application/octet-stream</c> server-side when null.</summary>
    public string? ContentType { get; init; }

    /// <summary>Marks the attachment inline, referenced from the HTML via <c>cid:</c>.</summary>
    public string? ContentId { get; init; }

    /// <summary>Creates a regular (non-inline) attachment from raw bytes.</summary>
    public static Attachment Create(string filename, byte[] content, string? contentType = null) =>
        new() { Filename = filename, Content = content, ContentType = contentType };

    /// <summary>Creates a regular (non-inline) attachment from already-base64-encoded content.</summary>
    public static Attachment FromBase64(string filename, string contentBase64, string? contentType = null) =>
        new() { Filename = filename, ContentBase64 = contentBase64, ContentType = contentType };

    /// <summary>Creates an inline attachment referenced from the HTML body via <c>cid:contentId</c>.</summary>
    public static Attachment Inline(string filename, byte[] content, string contentId, string? contentType = null) =>
        new() { Filename = filename, Content = content, ContentId = contentId, ContentType = contentType };

    /// <summary>
    /// Creates an inline attachment from already-base64-encoded content, referenced
    /// from the HTML body via <c>cid:contentId</c>.
    /// </summary>
    public static Attachment InlineFromBase64(
        string filename,
        string contentBase64,
        string contentId,
        string? contentType = null) =>
        new()
        {
            Filename = filename,
            ContentBase64 = contentBase64,
            ContentId = contentId,
            ContentType = contentType,
        };

    /// <summary>
    /// The base64 string to put on the wire. Throws when the two content
    /// properties are set together or not at all, which is a construction
    /// mistake the object-initializer syntax cannot catch at compile time.
    /// </summary>
    internal string WireContent()
    {
        if (Content is not null && ContentBase64 is not null)
        {
            throw new InvalidOperationException(
                $"Attachment '{Filename}' sets both Content and ContentBase64. Set exactly one.");
        }
        if (ContentBase64 is not null)
        {
            return ContentBase64;
        }
        if (Content is not null)
        {
            return Convert.ToBase64String(Content);
        }
        throw new InvalidOperationException(
            $"Attachment '{Filename}' has no content. Set Content (raw bytes) or ContentBase64.");
    }
}

/// <summary>
/// Overrides the sending domain's open/click tracking defaults for one message. A
/// null field leaves that dimension at the domain default.
/// </summary>
public sealed record Tracking
{
    /// <summary>Inject the open-tracking pixel into the HTML body when non-null.</summary>
    public bool? Opens { get; init; }

    /// <summary>Rewrite links for click tracking when non-null.</summary>
    public bool? Clicks { get; init; }

    /// <summary>Convenience factory for one or both tracking dimensions.</summary>
    public static Tracking Of(bool? opens = null, bool? clicks = null) =>
        new() { Opens = opens, Clicks = clicks };
}

/// <summary>Configures one-click unsubscribe headers for a send.</summary>
public sealed record Unsubscribe
{
    public required UnsubscribeMode Mode { get; init; }

    /// <summary>The human-readable label rendered on the hosted confirmation page.</summary>
    public string? DisplayName { get; init; }

    /// <summary>Mint a signed token and inject RFC 8058 unsubscribe headers. Requires a topic on the send.</summary>
    public static Unsubscribe Generate(string? displayName = null) =>
        new() { Mode = UnsubscribeMode.Generate, DisplayName = displayName };

    /// <summary>Inject no unsubscribe headers.</summary>
    public static Unsubscribe None() => new() { Mode = UnsubscribeMode.None };
}
