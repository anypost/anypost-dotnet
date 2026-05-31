namespace Anypost.Models;

/// <summary>
/// A reusable email template. The content fields hold the published content and
/// are null until first published; edits land in a draft, and
/// <c>Publish</c> promotes it. Sends always use the published content.
/// </summary>
public sealed record Template
{
    /// <summary>The <c>template_</c>-prefixed id.</summary>
    public string Id { get; init; } = "";

    /// <summary>The identifier, unique within the team.</summary>
    public string Name { get; init; } = "";

    /// <summary>The published subject line, null until first published.</summary>
    public string? Subject { get; init; }

    public TemplateKind Kind { get; init; }

    /// <summary>The published HTML body, null until first published.</summary>
    public string? Html { get; init; }

    /// <summary>The published, machine-derived plain-text body, null until first published.</summary>
    public string? Text { get; init; }

    /// <summary>The published emailmd source, set only for <c>kind=markdown</c>.</summary>
    public string? Markdown { get; init; }

    /// <summary>Whether an unpublished draft is pending.</summary>
    public bool HasDraft { get; init; }

    /// <summary>When last published, or null if never.</summary>
    public string? PublishedAt { get; init; }

    public string CreatedAt { get; init; } = "";

    public string UpdatedAt { get; init; } = "";
}

/// <summary>The unpublished draft content for a template.</summary>
public sealed record TemplateDraft
{
    public string? Subject { get; init; }

    public string? Html { get; init; }

    /// <summary>Always machine-derived from the draft's HTML/Markdown.</summary>
    public string? Text { get; init; }

    public string? Markdown { get; init; }

    public string UpdatedAt { get; init; } = "";
}

/// <summary>
/// The body for creating a template. The new template starts unpublished. For
/// <c>kind=html</c> supply <see cref="Html"/>; for <c>kind=markdown</c> supply
/// <see cref="Markdown"/>. The plain-text body is always derived server-side.
/// </summary>
public sealed record TemplateCreateParams
{
    public required string Name { get; init; }

    public string? Subject { get; init; }

    /// <summary>Defaults to <c>html</c> server-side and is immutable once the template exists.</summary>
    public TemplateKind? Kind { get; init; }

    public string? Html { get; init; }

    public string? Markdown { get; init; }
}

/// <summary>The body for updating a template. Only the name is mutable; content is draft-versioned.</summary>
public sealed record TemplateUpdateParams
{
    public required string Name { get; init; }
}

/// <summary>
/// The body for creating or updating a template draft. For <c>kind=html</c> supply
/// <see cref="Html"/>; for <c>kind=markdown</c> supply <see cref="Markdown"/>.
/// </summary>
public sealed record TemplateDraftParams
{
    public string? Subject { get; init; }

    public string? Html { get; init; }

    public string? Markdown { get; init; }
}

/// <summary>The body for duplicating a template.</summary>
public sealed record TemplateDuplicateParams
{
    /// <summary>Name for the copy. Defaults to <c>"&lt;source name&gt; (copy)"</c> when null.</summary>
    public string? Name { get; init; }
}
