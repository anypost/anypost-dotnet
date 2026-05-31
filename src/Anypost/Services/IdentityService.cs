using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Anypost.Internal;
using Anypost.Models;

namespace Anypost.Services;

/// <summary>The <c>/whoami</c> operation.</summary>
public sealed class IdentityService
{
    private readonly RequestExecutor _http;

    internal IdentityService(RequestExecutor http) => _http = http;

    /// <summary>Identifies the team and permission level behind the current API key.</summary>
    public Task<WhoamiResponse> WhoamiAsync(
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.SendAsync<WhoamiResponse>(HttpMethod.Get, "/whoami", body: null, idempotent: false, query: null, options, cancellationToken);
}
