using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Anypost;

/// <summary>
/// The single exception type thrown by every Anypost call that fails. A request
/// that reached the API and came back non-2xx carries <see cref="Type"/>,
/// <see cref="StatusCode"/>, and (when sent) <see cref="RequestId"/>; a request
/// that never got a response carries <see cref="ErrorType.Connection"/>, a null
/// <see cref="StatusCode"/>, and an inner exception.
/// </summary>
/// <remarks>
/// Recover it and branch on <see cref="Type"/>:
/// <code>
/// try { await client.Email.SendAsync(request); }
/// catch (AnypostException ex) when (ex.Type == ErrorType.Validation)
/// {
///     foreach (var (field, problems) in ex.ValidationErrors!) { /* ... */ }
/// }
/// </code>
/// </remarks>
public sealed class AnypostException : Exception
{
    internal AnypostException(
        ErrorType type,
        string message,
        int? statusCode = null,
        string? requestId = null,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? validationErrors = null,
        TimeSpan? retryAfter = null,
        string? body = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Type = type;
        StatusCode = statusCode;
        RequestId = requestId;
        ValidationErrors = validationErrors;
        RetryAfter = retryAfter;
        Body = body;
    }

    /// <summary>The stable, machine-readable error type. Branch on this.</summary>
    public ErrorType Type { get; }

    /// <summary>The HTTP status code, or null when no response was received.</summary>
    public int? StatusCode { get; }

    /// <summary>The server-assigned request id, when the response carried one. Quote it in support requests.</summary>
    public string? RequestId { get; }

    /// <summary>
    /// Field path to its list of problems. Populated only for
    /// <see cref="ErrorType.Validation"/>.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? ValidationErrors { get; }

    /// <summary>
    /// The server-advised wait before retrying. Populated only for
    /// <see cref="ErrorType.RateLimit"/> when the response carried a
    /// <c>Retry-After</c> header.
    /// </summary>
    public TimeSpan? RetryAfter { get; }

    /// <summary>The raw response body, for inspection beyond the parsed fields.</summary>
    public string? Body { get; }

    internal static AnypostException Connection(string message, Exception? inner) =>
        new(ErrorType.Connection, message, innerException: inner);

    /// <summary>
    /// Maps an HTTP error response into an <see cref="AnypostException"/>. Keys
    /// primarily on the canonical <c>error.type</c>, falling back to the status
    /// when the type is absent or unrecognized.
    /// </summary>
    internal static AnypostException FromResponse(int status, byte[] body, string? requestId, TimeSpan? retryAfter)
    {
        string? wireType = null;
        string? message = null;
        IReadOnlyDictionary<string, IReadOnlyList<string>>? fields = null;

        try
        {
            using var doc = JsonDocument.Parse(body.Length == 0 ? "{}"u8.ToArray() : body);
            var root = doc.RootElement;
            if (root.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == JsonValueKind.Object)
                {
                    // Canonical envelope: {"error": {type, message, errors?}}.
                    if (error.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String)
                    {
                        wireType = t.GetString();
                    }

                    if (error.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                    {
                        message = m.GetString();
                    }

                    if (error.TryGetProperty("errors", out var e) && e.ValueKind == JsonValueKind.Object)
                    {
                        fields = ParseFieldErrors(e);
                    }
                }
                else if (error.ValueKind == JsonValueKind.String)
                {
                    // Flat envelope: {"error": "<code>", "message"?}.
                    wireType = error.GetString();
                    if (root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                    {
                        message = m.GetString();
                    }

                    if (string.IsNullOrEmpty(message) && wireType is not null)
                    {
                        message = wireType.Replace('_', ' ');
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Non-JSON or malformed error body; fall back to status-derived values.
        }

        var type = wireType is not null ? ErrorTypes.Parse(wireType) : TypeFromStatus(status);
        if (string.IsNullOrEmpty(message))
        {
            message = string.Format(CultureInfo.InvariantCulture, "Anypost request failed with status {0}.", status);
        }

        return new AnypostException(
            type,
            message!,
            statusCode: status,
            requestId: requestId,
            validationErrors: type == ErrorType.Validation ? fields : null,
            retryAfter: type == ErrorType.RateLimit ? retryAfter : null,
            body: DecodeBody(body));
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> ParseFieldErrors(JsonElement errors)
    {
        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var property in errors.EnumerateObject())
        {
            var problems = new List<string>();
            if (property.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in property.Value.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        problems.Add(item.GetString()!);
                    }
                }
            }
            else if (property.Value.ValueKind == JsonValueKind.String)
            {
                problems.Add(property.Value.GetString()!);
            }

            result[property.Name] = problems;
        }

        return result;
    }

    private static ErrorType TypeFromStatus(int status) => status switch
    {
        401 => ErrorType.Authentication,
        403 => ErrorType.Permission,
        404 => ErrorType.NotFound,
        409 => ErrorType.Conflict,
        413 => ErrorType.PayloadTooLarge,
        429 => ErrorType.RateLimit,
        400 or 422 => ErrorType.Validation,
        >= 500 => ErrorType.Internal,
        _ => ErrorType.ApiError,
    };

    private static string? DecodeBody(byte[] body) =>
        body.Length == 0 ? null : Encoding.UTF8.GetString(body);
}
