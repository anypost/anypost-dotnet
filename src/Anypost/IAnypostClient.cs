using System.Threading;
using System.Threading.Tasks;
using Anypost.Models;
using Anypost.Services;

namespace Anypost;

/// <summary>
/// Abstraction over <see cref="AnypostClient"/>, for dependency injection and for
/// layering consumer code against an interface. Resolve it from the container
/// after <see cref="AnypostServiceCollectionExtensions.AddAnypost(Microsoft.Extensions.DependencyInjection.IServiceCollection, System.Action{AnypostClientOptions})"/>.
/// </summary>
/// <remarks>
/// Behavioral tests should drive the client through a fake
/// <see cref="System.Net.Http.HttpMessageHandler"/> (which exercises real
/// serialization, retries, and error mapping) rather than mocking this interface's
/// service objects.
/// </remarks>
public interface IAnypostClient
{
    /// <summary>Send operations (<c>/email</c>, <c>/email/batch</c>).</summary>
    EmailService Email { get; }

    /// <summary>Sending-domain operations (<c>/domains</c>).</summary>
    DomainsService Domains { get; }

    /// <summary>API-key operations (<c>/api-keys</c>).</summary>
    ApiKeysService ApiKeys { get; }

    /// <summary>Template operations (<c>/templates</c>), including draft/publish.</summary>
    TemplatesService Templates { get; }

    /// <summary>Suppression-list operations (<c>/suppressions</c>).</summary>
    SuppressionsService Suppressions { get; }

    /// <summary>Webhook operations (<c>/webhooks</c>), including test and rotation.</summary>
    WebhooksService Webhooks { get; }

    /// <summary>Read access to the event stream (<c>/events</c>).</summary>
    EventsService Events { get; }

    /// <summary>Identifies the team and permission level behind the current API key.</summary>
    Task<WhoamiResponse> WhoamiAsync(RequestOptions? options = null, CancellationToken cancellationToken = default);
}
