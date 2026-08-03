namespace Anypost.Models;

/// <summary>The identity resolved from the request's API key.</summary>
public sealed record WhoamiResponse
{
    /// <summary>The team the key belongs to, or null if it could not be resolved.</summary>
    public WhoamiTeam? Team { get; init; }

    /// <summary>The key on the request.</summary>
    public WhoamiApiKey ApiKey { get; init; } = new();

    /// <summary>
    /// The sending limits in force for the team, or null if it could not be resolved.
    /// </summary>
    public WhoamiLimits? Limits { get; init; }
}

/// <summary>
/// The sending limits currently enforced against a team. These are effective
/// values and can differ from the plan defaults, so read them at runtime rather
/// than hardcoding them.
/// </summary>
public sealed record WhoamiLimits
{
    /// <summary>
    /// Messages the team may send per calendar day (UTC). Exceeding it returns
    /// 429 with scope <c>daily</c>.
    /// </summary>
    public int Daily { get; init; }

    /// <summary>
    /// Messages the team may send per billing month. Exceeding it returns 429
    /// with scope <c>monthly</c>, unless prepaid overage credits cover the
    /// excess; those are not counted here.
    /// </summary>
    public int Monthly { get; init; }

    /// <summary>
    /// How fast accepted mail is released to receiving servers. Not a request
    /// limit and never a rejection: mail beyond this rate queues and drains at
    /// the metered rate.
    /// </summary>
    public int DeliveryRatePerMinute { get; init; }
}

/// <summary>Identifies the team behind the API key.</summary>
public sealed record WhoamiTeam
{
    public string Id { get; init; } = "";

    public string Name { get; init; } = "";
}

/// <summary>Identifies the API key on the request.</summary>
public sealed record WhoamiApiKey
{
    public string Id { get; init; } = "";

    public Permissions Permissions { get; init; }
}
