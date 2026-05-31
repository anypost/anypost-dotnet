using System;
using Microsoft.Extensions.DependencyInjection;

namespace Anypost;

/// <summary>
/// Registers <see cref="IAnypostClient"/> in a dependency-injection container as an
/// <see cref="System.Net.Http.IHttpClientFactory"/>-managed typed client.
/// </summary>
public static class AnypostServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IAnypostClient"/> with the given configuration:
    /// <code>
    /// services.AddAnypost(o =>
    /// {
    ///     o.ApiKey = builder.Configuration["Anypost:ApiKey"];
    /// });
    /// </code>
    /// Then inject <see cref="IAnypostClient"/> wherever you send mail. The returned
    /// <see cref="IHttpClientBuilder"/> lets you further configure the transport
    /// (primary handler, Polly policies, and so on).
    /// </summary>
    public static IHttpClientBuilder AddAnypost(this IServiceCollection services, Action<AnypostClientOptions> configure)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        services.Configure(configure);
        return services.AddHttpClient<IAnypostClient, AnypostClient>();
    }

    /// <summary>
    /// Registers <see cref="IAnypostClient"/> with default options, reading the API
    /// key from the <c>ANYPOST_API_KEY</c> environment variable.
    /// </summary>
    public static IHttpClientBuilder AddAnypost(this IServiceCollection services) =>
        services.AddAnypost(static _ => { });
}
