using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Anypost;

/// <summary>
/// Configuration for an <see cref="AnypostClient"/>. Usable as a plain options
/// object (<c>new AnypostClientOptions { ... }</c>) or bound through the options
/// pattern via <see cref="AnypostServiceCollectionExtensions.AddAnypost(Microsoft.Extensions.DependencyInjection.IServiceCollection, Action{AnypostClientOptions})"/>.
/// </summary>
public sealed class AnypostClientOptions
{
    /// <summary>
    /// The API key. Optional when constructing directly with an explicit key, or
    /// when the <c>ANYPOST_API_KEY</c> environment variable is set. Required when
    /// registering through dependency injection without that environment variable.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>The API base URL. Defaults to the production endpoint.</summary>
    public string BaseUrl { get; set; } = "https://api.anypost.com/v1";

    /// <summary>
    /// The per-request timeout. Defaults to 30 seconds. A zero or negative value
    /// disables the client-imposed timeout (a passed
    /// <see cref="System.Threading.CancellationToken"/> still applies).
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The number of automatic retries for transient failures (429/502/503 and
    /// network errors). Defaults to 2. Set 0 to disable.
    /// </summary>
    public int MaxRetries { get; set; } = 2;

    /// <summary>
    /// A custom <see cref="System.Net.Http.HttpClient"/>. Supply one to configure a
    /// proxy, custom TLS, or a test handler. When set, the caller owns its lifetime;
    /// otherwise the client creates and disposes its own. Ignored when the client is
    /// registered through dependency injection — there the
    /// <see cref="System.Net.Http.IHttpClientFactory"/> supplies and manages it.
    /// </summary>
    public HttpClient? HttpClient { get; set; }

    /// <summary>Headers sent on every request.</summary>
    public IReadOnlyDictionary<string, string>? DefaultHeaders { get; set; }
}
