using System.Collections.Generic;

namespace Anypost.Models;

/// <summary>An API key's metadata. The plaintext secret is never returned here.</summary>
public record ApiKey
{
    /// <summary>The <c>key_</c>-prefixed id.</summary>
    public string Id { get; init; } = "";

    public string Name { get; init; } = "";

    /// <summary>The first 12 characters of the key, shown for identification.</summary>
    public string KeyPrefix { get; init; } = "";

    public Permissions Permissions { get; init; }

    /// <summary>The domains this key may send from. Null means all verified domains.</summary>
    public IReadOnlyList<string>? AllowedDomains { get; init; }

    /// <summary>The IPs/CIDRs allowed to use this key. Null means all IPs.</summary>
    public IReadOnlyList<string>? AllowedIps { get; init; }

    /// <summary>When the key was last used, or null if never.</summary>
    public string? LastUsedAt { get; init; }

    public string CreatedAt { get; init; } = "";
}

/// <summary>
/// A newly created key, including its plaintext secret. The secret is returned
/// only once, at creation.
/// </summary>
public sealed record ApiKeyWithSecret : ApiKey
{
    /// <summary>The full API key. Store it securely; it cannot be retrieved later.</summary>
    public string Key { get; init; } = "";
}

/// <summary>The body for creating an API key.</summary>
public sealed record ApiKeyCreateParams
{
    public required string Name { get; init; }

    public required Permissions Permissions { get; init; }

    /// <summary>Restricts sending to these domains. Omit for all verified domains.</summary>
    public IReadOnlyList<string>? AllowedDomains { get; init; }

    /// <summary>Restricts use to these IPs/CIDRs. Omit for all IPs.</summary>
    public IReadOnlyList<string>? AllowedIps { get; init; }
}

/// <summary>The body for updating an API key. The plaintext secret is not rotated here.</summary>
public sealed record ApiKeyUpdateParams
{
    public required string Name { get; init; }

    public required Permissions Permissions { get; init; }

    /// <summary>Restricts sending to these domains. Pass an empty list to lift the restriction.</summary>
    public IReadOnlyList<string>? AllowedDomains { get; init; }

    /// <summary>Restricts use to these IPs/CIDRs. Pass an empty list to lift it.</summary>
    public IReadOnlyList<string>? AllowedIps { get; init; }
}
