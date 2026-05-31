using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Anypost.Internal;
using Anypost.Models;
using Anypost.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Anypost;

/// <summary>
/// The entry point to the Anypost API. Construct it with an API key (or set
/// <c>ANYPOST_API_KEY</c> and call <see cref="FromEnv"/>), then call resource
/// methods. Every method is asynchronous and accepts a
/// <see cref="CancellationToken"/>.
/// </summary>
/// <remarks>
/// <code>
/// var client = AnypostClient.Create("ap_your_api_key");
/// var sent = await client.Email.SendAsync(new SendEmailRequest
/// {
///     From = "Acme &lt;you@yourdomain.com&gt;",
///     To = ["someone@example.com"],
///     Subject = "Hello",
///     Html = "&lt;p&gt;It worked.&lt;/p&gt;",
/// });
/// </code>
/// In an ASP.NET Core or Worker host, prefer
/// <see cref="AnypostServiceCollectionExtensions.AddAnypost(IServiceCollection, Action{AnypostClientOptions})"/>
/// and inject <see cref="IAnypostClient"/> — the <see cref="IHttpClientFactory"/>
/// then manages the underlying <see cref="HttpClient"/>.
///
/// A failed call throws an <see cref="AnypostException"/>; branch on its
/// <see cref="AnypostException.Type"/>. The client is safe for concurrent use; keep
/// the API key server-side.
/// </remarks>
public sealed class AnypostClient : IAnypostClient, IDisposable
{
    private const string EnvApiKey = "ANYPOST_API_KEY";

    private readonly HttpClient? _ownedHttpClient;
    private readonly IdentityService _identity;

    /// <summary>Creates a client. If <paramref name="apiKey"/> is empty, falls back to <c>ANYPOST_API_KEY</c>.</summary>
    /// <exception cref="ArgumentException">No API key was provided and the environment variable is unset.</exception>
    public AnypostClient(string? apiKey, AnypostClientOptions? options = null)
        : this(apiKey, options ?? new AnypostClientOptions(), factoryHttpClient: null)
    {
    }

    /// <summary>
    /// Constructor used by the <see cref="IHttpClientFactory"/> typed-client
    /// registration from <c>AddAnypost</c>. The factory supplies and manages
    /// <paramref name="httpClient"/>; configuration comes from the bound options.
    /// </summary>
    [ActivatorUtilitiesConstructor]
    public AnypostClient(HttpClient httpClient, IOptions<AnypostClientOptions> options)
        : this(apiKey: null, options.Value, factoryHttpClient: httpClient)
    {
    }

    private AnypostClient(string? apiKey, AnypostClientOptions options, HttpClient? factoryHttpClient)
    {
        var key = !string.IsNullOrEmpty(apiKey) ? apiKey
            : !string.IsNullOrEmpty(options.ApiKey) ? options.ApiKey
            : Environment.GetEnvironmentVariable(EnvApiKey);
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException(
                "An API key is required; pass it to the constructor, set " + nameof(AnypostClientOptions.ApiKey) +
                ", or set the " + EnvApiKey + " environment variable.", nameof(apiKey));
        }

        HttpClient http;
        if (factoryHttpClient is not null)
        {
            // Supplied and owned by IHttpClientFactory — do not dispose it here.
            http = factoryHttpClient;
        }
        else if (options.HttpClient is not null)
        {
            // Supplied by the caller — their lifetime to manage.
            http = options.HttpClient;
        }
        else
        {
            _ownedHttpClient = new HttpClient();
            http = _ownedHttpClient;
        }

        var executor = new RequestExecutor(
            http,
            key!,
            options.BaseUrl,
            options.MaxRetries,
            options.Timeout,
            options.DefaultHeaders ?? new Dictionary<string, string>(),
            BuildUserAgent());

        Executor = executor;
        Email = new EmailService(executor);
        Domains = new DomainsService(executor);
        ApiKeys = new ApiKeysService(executor);
        Templates = new TemplatesService(executor);
        Suppressions = new SuppressionsService(executor);
        Webhooks = new WebhooksService(executor);
        Events = new EventsService(executor);
        _identity = new IdentityService(executor);
    }

    /// <inheritdoc/>
    public EmailService Email { get; }

    /// <inheritdoc/>
    public DomainsService Domains { get; }

    /// <inheritdoc/>
    public ApiKeysService ApiKeys { get; }

    /// <inheritdoc/>
    public TemplatesService Templates { get; }

    /// <inheritdoc/>
    public SuppressionsService Suppressions { get; }

    /// <inheritdoc/>
    public WebhooksService Webhooks { get; }

    /// <inheritdoc/>
    public EventsService Events { get; }

    internal RequestExecutor Executor { get; }

    /// <summary>Creates a client with the given API key.</summary>
    public static AnypostClient Create(string apiKey, AnypostClientOptions? options = null) =>
        new(apiKey, options);

    /// <summary>Creates a client reading the API key from the <c>ANYPOST_API_KEY</c> environment variable.</summary>
    public static AnypostClient FromEnv(AnypostClientOptions? options = null) =>
        new(apiKey: null, options);

    /// <inheritdoc/>
    public Task<WhoamiResponse> WhoamiAsync(
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _identity.WhoamiAsync(options, cancellationToken);

    /// <summary>Disposes the internally created <see cref="HttpClient"/>, if any.</summary>
    public void Dispose() => _ownedHttpClient?.Dispose();

    private static string BuildUserAgent()
    {
        var assembly = typeof(AnypostClient).Assembly;
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var version = informational ?? assembly.GetName().Version?.ToString() ?? "0.0.0";

        // Strip source-control build metadata (e.g. "0.1.0+abcdef").
        var plus = version.IndexOf('+');
        if (plus >= 0)
        {
            version = version[..plus];
        }

        return $"anypost-dotnet/{version} {RuntimeInformation.FrameworkDescription}";
    }
}
