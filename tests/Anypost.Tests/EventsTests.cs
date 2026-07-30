using System.Net;
using System.Threading.Tasks;
using Anypost.Models;
using Xunit;

namespace Anypost.Tests;

public class EventsTests
{
    [Fact]
    public async Task List_exposes_bot_on_a_proxied_open()
    {
        using var client = TestClient.Create(out var handler, out _);
        handler.Enqueue(HttpStatusCode.OK, """
        {"data":[
            {"id":"evt_bot","type":"email.opened","tracking":{"bot":{"source":"google","kind":"proxy"}}},
            {"id":"evt_human","type":"email.opened","tracking":null}
        ],"has_more":false,"next_cursor":null}
        """);

        var page = await client.Events.ListAsync(new EventListParams { EventType = EventType.Opened });

        // Mirrors the webhook payload's data.tracking.bot.
        Assert.Equal("google", page.Data[0].Tracking!.Bot!.Source);
        Assert.Equal("proxy", page.Data[0].Tracking!.Bot!.Kind);
        // A human open carries no bot classification.
        Assert.Null(page.Data[1].Tracking);
    }

    [Fact]
    public async Task List_threads_event_type_and_tags_into_the_query()
    {
        using var client = TestClient.Create(out var handler, out _);
        handler.Enqueue(HttpStatusCode.OK, """{"data":[],"has_more":false,"next_cursor":null}""");

        await client.Events.ListAsync(new EventListParams
        {
            EventType = EventType.Bounced,
            Tags = new[] { "welcome", "onboarding" },
        });

        var query = handler.Requests[0].RequestUri.Query;
        Assert.Contains("event_type=email.bounced", query);
        // Sent comma-separated (URL-encoded); the API matches with has-any.
        Assert.Contains("tags=welcome%2Conboarding", query);
    }

    [Fact]
    public async Task List_threads_ip_pool_into_the_query()
    {
        using var client = TestClient.Create(out var handler, out _);
        handler.Enqueue(HttpStatusCode.OK, """{"data":[],"has_more":false,"next_cursor":null}""");

        await client.Events.ListAsync(new EventListParams { IpPool = "marketing" });

        Assert.Contains("ip_pool=marketing", handler.Requests[0].RequestUri.Query);
    }

    [Fact]
    public async Task Ip_pool_deserializes_off_an_event()
    {
        using var client = TestClient.Create(out var handler, out _);
        handler.Enqueue(HttpStatusCode.OK, """
        {"data":[
            {"id":"evt_pooled","type":"email.delivered","ip_pool":"marketing"},
            {"id":"evt_shared","type":"email.delivered","ip_pool":null}
        ],"has_more":false,"next_cursor":null}
        """);

        var page = await client.Events.ListAsync(new EventListParams());

        Assert.Equal("marketing", page.Data[0].IpPool);
        // Null on sends that named no pool, and on accounts without dedicated IPs.
        Assert.Null(page.Data[1].IpPool);
    }
}
