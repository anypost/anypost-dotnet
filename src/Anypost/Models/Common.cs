namespace Anypost.Models;

/// <summary>
/// One attachment on a message. <see cref="Content"/> is the raw file bytes; the
/// SDK base64-encodes it on the wire — do not pre-encode it.
/// </summary>
public sealed record Attachment
{
    /// <summary>The file name shown to the recipient.</summary>
    public required string Filename { get; init; }

    /// <summary>The raw file bytes; encoded to base64 on the wire.</summary>
    public required byte[] Content { get; init; }

    /// <summary>The MIME type. Defaults to <c>application/octet-stream</c> server-side when null.</summary>
    public string? ContentType { get; init; }

    /// <summary>Marks the attachment inline, referenced from the HTML via <c>cid:</c>.</summary>
    public string? ContentId { get; init; }

    /// <summary>Creates a regular (non-inline) attachment.</summary>
    public static Attachment Create(string filename, byte[] content, string? contentType = null) =>
        new() { Filename = filename, Content = content, ContentType = contentType };

    /// <summary>Creates an inline attachment referenced from the HTML body via <c>cid:contentId</c>.</summary>
    public static Attachment Inline(string filename, byte[] content, string contentId, string? contentType = null) =>
        new() { Filename = filename, Content = content, ContentId = contentId, ContentType = contentType };
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
