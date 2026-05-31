namespace Anypost.Models;

/// <summary>The identity resolved from the request's API key.</summary>
public sealed record WhoamiResponse
{
    /// <summary>The team the key belongs to, or null if it could not be resolved.</summary>
    public WhoamiTeam? Team { get; init; }

    /// <summary>The key on the request.</summary>
    public WhoamiApiKey ApiKey { get; init; } = new();
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
