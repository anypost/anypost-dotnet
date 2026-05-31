using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Anypost;

/// <summary>
/// One page of a list result. It mirrors the wire envelope
/// (<see cref="Data"/>, <see cref="HasMore"/>, <see cref="NextCursor"/>); call
/// <see cref="NextAsync"/> to fetch the following page, or
/// <c>await foreach</c> over <see cref="AllAsync"/> to walk every remaining item
/// across pages.
/// </summary>
public sealed class Page<T>
{
    private readonly Func<string, CancellationToken, Task<Page<T>>> _fetch;

    internal Page(
        IReadOnlyList<T> data,
        bool hasMore,
        string? nextCursor,
        Func<string, CancellationToken, Task<Page<T>>> fetch)
    {
        Data = data;
        HasMore = hasMore;
        NextCursor = nextCursor;
        _fetch = fetch;
    }

    /// <summary>The items on this page.</summary>
    public IReadOnlyList<T> Data { get; }

    /// <summary>Whether another page exists.</summary>
    public bool HasMore { get; }

    /// <summary>
    /// The cursor for the next page, or null when there are none. Pass it back as
    /// <see cref="ListParams.After"/> to fetch that page yourself.
    /// </summary>
    public string? NextCursor { get; }

    /// <summary>Fetches the following page, or returns null when there are none.</summary>
    public Task<Page<T>?> NextAsync(CancellationToken cancellationToken = default)
    {
        if (!HasMore || string.IsNullOrEmpty(NextCursor))
        {
            return Task.FromResult<Page<T>?>(null);
        }

        return Fetch(NextCursor!, cancellationToken);
    }

    /// <summary>
    /// Iterates every item across this and all following pages, fetching as it
    /// goes:
    /// <code>
    /// await foreach (var domain in page.AllAsync()) { /* ... */ }
    /// </code>
    /// </summary>
    public async IAsyncEnumerable<T> AllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var page = this;
        while (page is not null)
        {
            foreach (var item in page.Data)
            {
                yield return item;
            }

            page = await page.NextAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<Page<T>?> Fetch(string cursor, CancellationToken cancellationToken) =>
        await _fetch(cursor, cancellationToken).ConfigureAwait(false);
}
