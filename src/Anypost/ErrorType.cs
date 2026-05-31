using System.Collections.Generic;

namespace Anypost;

/// <summary>
/// The stable, machine-readable classification of an API error. Branch on this
/// rather than the HTTP status: the type is part of the API contract, the status
/// is not.
/// </summary>
public enum ErrorType
{
    /// <summary>The request body or query failed validation (<c>400</c>/<c>422</c>).</summary>
    Validation,

    /// <summary>The API key is missing or invalid (<c>401</c>).</summary>
    Authentication,

    /// <summary>The key may not perform this action (<c>403</c>).</summary>
    Permission,

    /// <summary>No such resource for this team (<c>404</c>).</summary>
    NotFound,

    /// <summary>A generic request conflict (<c>409</c>).</summary>
    Conflict,

    /// <summary>A request with the same idempotency key is still in flight (<c>409</c>).</summary>
    IdempotencyConflict,

    /// <summary>An idempotency key was reused with a different body (<c>422</c>).</summary>
    IdempotencyMismatch,

    /// <summary>A webhook signing-secret rotation is already in progress (<c>409</c>).</summary>
    WebhookRotationInProgress,

    /// <summary>A rate limit was exceeded (<c>429</c>).</summary>
    RateLimit,

    /// <summary>The request body exceeded the size cap (<c>413</c>).</summary>
    PayloadTooLarge,

    /// <summary>Anypost could not complete a provisioning step (<c>503</c>).</summary>
    Provisioning,

    /// <summary>An unexpected server error (<c>5xx</c>).</summary>
    Internal,

    /// <summary>No HTTP response was received: a network failure, timeout, or cancellation.</summary>
    Connection,

    /// <summary>An error whose type was absent or unrecognized; fall back to <see cref="AnypostException.StatusCode"/>.</summary>
    ApiError,
}

internal static class ErrorTypes
{
    private static readonly Dictionary<string, ErrorType> FromWire = new()
    {
        ["validation_error"] = ErrorType.Validation,
        ["authentication_error"] = ErrorType.Authentication,
        ["permission_error"] = ErrorType.Permission,
        ["not_found"] = ErrorType.NotFound,
        ["conflict"] = ErrorType.Conflict,
        ["idempotency_concurrent"] = ErrorType.IdempotencyConflict,
        ["idempotency_mismatch"] = ErrorType.IdempotencyMismatch,
        ["webhook_rotation_in_progress"] = ErrorType.WebhookRotationInProgress,
        ["rate_limit_exceeded"] = ErrorType.RateLimit,
        ["payload_too_large"] = ErrorType.PayloadTooLarge,
        ["provisioning_error"] = ErrorType.Provisioning,
        ["internal_error"] = ErrorType.Internal,
    };

    internal static ErrorType Parse(string? wire) =>
        wire is not null && FromWire.TryGetValue(wire, out var value) ? value : ErrorType.ApiError;
}
