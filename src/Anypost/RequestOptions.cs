using System.Collections.Generic;

namespace Anypost;

/// <summary>
/// Per-call overrides. Pass to any service method to set an idempotency key or
/// add request headers for that one call.
/// </summary>
public sealed class RequestOptions
{
    /// <summary>
    /// The <c>Idempotency-Key</c> for a send. Reusing a key with an identical body
    /// replays the stored result; reusing it with a different body fails with
    /// <see cref="ErrorType.IdempotencyMismatch"/>. Only the send endpoints honor it.
    /// </summary>
    public string? IdempotencyKey { get; init; }

    /// <summary>Additional headers to set on this request, overriding defaults.</summary>
    public IReadOnlyDictionary<string, string>? Headers { get; init; }
}
