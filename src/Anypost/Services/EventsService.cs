using System.Threading;
using System.Threading.Tasks;
using Anypost.Internal;
using Anypost.Models;

namespace Anypost.Services;

/// <summary>
/// Read access to the <c>/events</c> stream. List-only — events are not
/// addressable by id. Access it via <see cref="AnypostClient.Events"/>.
/// </summary>
public sealed class EventsService
{
    private readonly RequestExecutor _http;

    internal EventsService(RequestExecutor http) => _http = http;

    /// <summary>Returns one page of the team's events, newest-first.</summary>
    public Task<Page<Event>> ListAsync(
        EventListParams? listParams = null,
        RequestOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _http.ListAsync<Event>("/events", listParams ?? new EventListParams(), options, cancellationToken);
}
