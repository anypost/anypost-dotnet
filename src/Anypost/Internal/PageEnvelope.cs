using System.Collections.Generic;

namespace Anypost.Internal;

/// <summary>The wire shape every list endpoint returns.</summary>
internal sealed record PageEnvelope<T>
{
    public IReadOnlyList<T> Data { get; init; } = new List<T>();

    public bool HasMore { get; init; }

    public string? NextCursor { get; init; }
}
