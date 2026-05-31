using System.Collections.Generic;

namespace Anypost.Models;

/// <summary>A DNS record the customer must publish to verify a domain or its branded tracking.</summary>
public sealed record DnsRecord
{
    /// <summary>The record type. <c>CNAME</c> is the only value today.</summary>
    public string Type { get; init; } = "";

    /// <summary>The record name to publish, relative to the registered apex.</summary>
    public string Name { get; init; } = "";

    /// <summary>The CNAME target (absolute FQDN).</summary>
    public string Value { get; init; } = "";

    /// <summary>One of <c>verification</c>, <c>dkim</c>, or <c>tracking</c>.</summary>
    public string Purpose { get; init; } = "";
}

/// <summary>A stable failure category plus a human-readable message.</summary>
public sealed record VerificationFailure
{
    /// <summary>A stable, switchable failure code.</summary>
    public string Code { get; init; } = "";

    /// <summary>A human-readable description with record names interpolated.</summary>
    public string Message { get; init; } = "";
}

/// <summary>
/// A domain's branded open/click tracking configuration. Independent of mail-flow
/// verification.
/// </summary>
public sealed record DomainTracking
{
    public bool OpensEnabled { get; init; }

    public bool ClicksEnabled { get; init; }

    /// <summary>The tracking subdomain prefix, or null when tracking is off.</summary>
    public string? Subdomain { get; init; }

    /// <summary>The branded-tracking records to publish. Empty when off.</summary>
    public IReadOnlyList<DnsRecord> DnsRecords { get; init; } = new List<DnsRecord>();

    /// <summary>One of <c>disabled</c>, <c>pending</c>, or <c>verified</c>.</summary>
    public string Status { get; init; } = "";

    /// <summary>The most recent tracking-CNAME failure, or null.</summary>
    public VerificationFailure? VerificationFailure { get; init; }

    /// <summary>When the tracking CNAME was last observed resolving, or null.</summary>
    public string? VerifiedAt { get; init; }
}

/// <summary>A sending domain and its mail-flow verification state.</summary>
public sealed record Domain
{
    /// <summary>The <c>domain_</c>-prefixed id.</summary>
    public string Id { get; init; } = "";

    /// <summary>The domain name, e.g. <c>example.com</c>.</summary>
    public string Name { get; init; } = "";

    /// <summary><c>pending</c> until the mail-flow CNAMEs resolve, then <c>verified</c>.</summary>
    public string Status { get; init; } = "";

    /// <summary>The mail-flow records to publish.</summary>
    public IReadOnlyList<DnsRecord> DnsRecords { get; init; } = new List<DnsRecord>();

    /// <summary>The most recent mail-flow failure, or null.</summary>
    public VerificationFailure? VerificationFailure { get; init; }

    /// <summary>The branded tracking configuration and its status.</summary>
    public DomainTracking Tracking { get; init; } = new();

    public string CreatedAt { get; init; } = "";

    /// <summary>When the domain last transitioned to verified, or null.</summary>
    public string? VerifiedAt { get; init; }
}

/// <summary>The body for adding a sending domain.</summary>
public sealed record DomainCreateParams
{
    /// <summary>The domain to add, e.g. <c>example.com</c>.</summary>
    public required string Name { get; init; }
}

/// <summary>
/// The mutable tracking configuration on a domain update. Leave a field null to
/// leave it unchanged.
/// </summary>
public sealed record DomainTrackingParams
{
    public bool? OpensEnabled { get; init; }

    public bool? ClicksEnabled { get; init; }

    /// <summary>
    /// The tracking subdomain prefix. Required when either tracking flag is turned
    /// on; leave null to keep the current value.
    /// </summary>
    public string? Subdomain { get; init; }
}

/// <summary>The body for a domain update. Only tracking configuration is mutable; the name is immutable.</summary>
public sealed record DomainUpdateParams
{
    public required DomainTrackingParams Tracking { get; init; }
}
