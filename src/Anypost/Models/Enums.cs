using Anypost.Internal;

namespace Anypost.Models;

/// <summary>The permission level of an API key.</summary>
public enum Permissions
{
    /// <summary>An unrecognized value returned by a newer server.</summary>
    Unknown = 0,

    /// <summary>Management and send access.</summary>
    [JsonStringValue("full")]
    Full,

    /// <summary>Send access only.</summary>
    [JsonStringValue("send_only")]
    SendOnly,
}

/// <summary>A template's authoring format. Immutable once a template exists.</summary>
public enum TemplateKind
{
    /// <summary>An unrecognized value returned by a newer server.</summary>
    Unknown = 0,

    [JsonStringValue("html")]
    Html,

    [JsonStringValue("markdown")]
    Markdown,
}

/// <summary>The one-click unsubscribe behavior for a send.</summary>
public enum UnsubscribeMode
{
    /// <summary>
    /// Mint a per-recipient signed token and inject RFC 8058 unsubscribe headers.
    /// Requires a topic on the send.
    /// </summary>
    [JsonStringValue("generate")]
    Generate,

    /// <summary>
    /// Inject nothing — for transactional sends that must not carry unsubscribe
    /// semantics.
    /// </summary>
    [JsonStringValue("none")]
    None,
}

/// <summary>An event type a webhook can subscribe to, or that appears in the event stream.</summary>
public enum WebhookEventType
{
    /// <summary>An unrecognized value returned by a newer server.</summary>
    Unknown = 0,

    [JsonStringValue("email.sent")]
    Sent,

    [JsonStringValue("email.delivered")]
    Delivered,

    [JsonStringValue("email.delayed")]
    Delayed,

    [JsonStringValue("email.bounced")]
    Bounced,

    [JsonStringValue("email.complained")]
    Complained,

    [JsonStringValue("email.suppressed")]
    Suppressed,

    [JsonStringValue("email.unsubscribed")]
    Unsubscribed,

    [JsonStringValue("email.opened")]
    Opened,

    [JsonStringValue("email.clicked")]
    Clicked,
}

/// <summary>
/// A webhook's delivery state. Only <see cref="Active"/> and <see cref="Disabled"/>
/// can be set through the API; <see cref="CircuitDisabled"/> is server-managed.
/// </summary>
public enum WebhookStatus
{
    /// <summary>An unrecognized value returned by a newer server.</summary>
    Unknown = 0,

    [JsonStringValue("active")]
    Active,

    [JsonStringValue("disabled")]
    Disabled,

    [JsonStringValue("circuit_disabled")]
    CircuitDisabled,
}

/// <summary>A customer-facing event type in the event stream.</summary>
public enum EventType
{
    /// <summary>An unrecognized value returned by a newer server.</summary>
    Unknown = 0,

    [JsonStringValue("email.sent")]
    Sent,

    [JsonStringValue("email.delivered")]
    Delivered,

    [JsonStringValue("email.delayed")]
    Delayed,

    [JsonStringValue("email.bounced")]
    Bounced,

    [JsonStringValue("email.complained")]
    Complained,

    [JsonStringValue("email.suppressed")]
    Suppressed,

    [JsonStringValue("email.unsubscribed")]
    Unsubscribed,

    [JsonStringValue("email.opened")]
    Opened,

    [JsonStringValue("email.clicked")]
    Clicked,
}

/// <summary>Why an address is suppressed.</summary>
public enum SuppressionReason
{
    /// <summary>An unrecognized value returned by a newer server.</summary>
    Unknown = 0,

    [JsonStringValue("permanent_bounce")]
    PermanentBounce,

    [JsonStringValue("complaint")]
    Complaint,

    [JsonStringValue("unsubscribed")]
    Unsubscribed,

    [JsonStringValue("manual")]
    Manual,
}

/// <summary>The provenance of a suppression row.</summary>
public enum SuppressionOrigin
{
    /// <summary>An unrecognized value returned by a newer server.</summary>
    Unknown = 0,

    [JsonStringValue("auto")]
    Auto,

    [JsonStringValue("manual")]
    Manual,
}
