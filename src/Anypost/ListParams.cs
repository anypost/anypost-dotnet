using Anypost.Internal;

namespace Anypost;

/// <summary>
/// The cursor-pagination parameters shared by every list endpoint. A null value
/// requests the first page at the server default size.
/// </summary>
public class ListParams
{
    /// <summary>Page size, 1 to 100. Null uses the server default (20).</summary>
    public int? Limit { get; init; }

    /// <summary>A cursor from a previous page's <see cref="Page{T}.NextCursor"/>. Opaque — do not parse.</summary>
    public string? After { get; init; }

    // Emits every filter except the cursor; the page-walker supplies "after"
    // so it can advance the cursor without rebuilding the params.
    internal virtual void Apply(Query query)
    {
        query.Add("limit", Limit);
    }
}
