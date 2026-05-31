using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Anypost;
using Anypost.Models;
using Xunit;

namespace Anypost.Tests;

public class PaginationTests
{
    [Fact]
    public async Task List_returns_a_page_and_carries_the_limit()
    {
        using var client = TestClient.Create(out var handler, out _);
        handler.Enqueue(HttpStatusCode.OK, """
        {"data":[{"id":"domain_1","name":"a.com","status":"verified"}],"has_more":true,"next_cursor":"c2"}
        """);

        var page = await client.Domains.ListAsync(new ListParams { Limit = 50 });

        Assert.Single(page.Data);
        Assert.Equal("domain_1", page.Data[0].Id);
        Assert.True(page.HasMore);
        Assert.Equal("c2", page.NextCursor);
        Assert.Contains("limit=50", handler.Requests[0].RequestUri.Query);
    }

    [Fact]
    public async Task NextAsync_advances_the_cursor()
    {
        using var client = TestClient.Create(out var handler, out _);
        handler.Enqueue(HttpStatusCode.OK, """{"data":[{"id":"domain_1","name":"a.com"}],"has_more":true,"next_cursor":"c2"}""");
        handler.Enqueue(HttpStatusCode.OK, """{"data":[{"id":"domain_2","name":"b.com"}],"has_more":false,"next_cursor":null}""");

        var page1 = await client.Domains.ListAsync(new ListParams { Limit = 50 });
        var page2 = await page1.NextAsync();

        Assert.NotNull(page2);
        Assert.Equal("domain_2", page2!.Data[0].Id);
        Assert.False(page2.HasMore);
        Assert.Null(await page2.NextAsync());

        Assert.Contains("after=c2", handler.Requests[1].RequestUri.Query);
        Assert.Contains("limit=50", handler.Requests[1].RequestUri.Query);
    }

    [Fact]
    public async Task AllAsync_walks_every_page()
    {
        using var client = TestClient.Create(out var handler, out _);
        handler.Enqueue(HttpStatusCode.OK, """{"data":[{"id":"domain_1","name":"a.com"}],"has_more":true,"next_cursor":"c2"}""");
        handler.Enqueue(HttpStatusCode.OK, """{"data":[{"id":"domain_2","name":"b.com"}],"has_more":false,"next_cursor":null}""");

        var page = await client.Domains.ListAsync();
        var ids = new List<string>();
        await foreach (var domain in page.AllAsync())
        {
            ids.Add(domain.Id);
        }

        Assert.Equal(new[] { "domain_1", "domain_2" }, ids);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Suppression_list_filters_reach_the_query()
    {
        using var client = TestClient.Create(out var handler, out _);
        handler.Enqueue(HttpStatusCode.OK, """{"data":[],"has_more":false,"next_cursor":null}""");

        await client.Suppressions.ListAsync(new SuppressionListParams
        {
            Limit = 10,
            Reason = SuppressionReason.Complaint,
            Origin = SuppressionOrigin.Manual,
            Topic = "newsletter",
        });

        var query = handler.Requests[0].RequestUri.Query;
        Assert.Contains("limit=10", query);
        Assert.Contains("reason=complaint", query);
        Assert.Contains("origin=manual", query);
        Assert.Contains("topic=newsletter", query);
    }
}
