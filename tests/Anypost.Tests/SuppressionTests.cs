using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Anypost;
using Anypost.Models;
using Xunit;

namespace Anypost.Tests;

public class SuppressionTests
{
    [Fact]
    public async Task CreateBatch_posts_the_wrapped_items_and_parses_the_result()
    {
        using var client = TestClient.Create(out var handler, out _);
        handler.Enqueue(HttpStatusCode.OK, """{"imported":1,"already_suppressed":0,"invalid":[{"index":1,"email":"not-an-address"}]}""");

        var result = await client.Suppressions.CreateBatchAsync([
            new SuppressionBatchItem { Email = "alice@example.com", Topic = "marketing", Reason = "hard bounce", Note = "imported" },
            new SuppressionBatchItem { Email = "not-an-address" },
        ]);

        Assert.Equal(1, result.Imported);
        Assert.Equal(0, result.AlreadySuppressed);
        var invalid = Assert.Single(result.Invalid);
        Assert.Equal(1, invalid.Index);
        Assert.Equal("not-an-address", invalid.Email);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("POST", request.Method.Method);
        Assert.Equal("https://api.test/v1/suppressions/batch", request.RequestUri.ToString());

        using var body = JsonDocument.Parse(request.Body!);
        var items = body.RootElement.GetProperty("suppressions");
        Assert.Equal(2, items.GetArrayLength());
        Assert.Equal("alice@example.com", items[0].GetProperty("email").GetString());
        Assert.Equal("marketing", items[0].GetProperty("topic").GetString());
        Assert.Equal("hard bounce", items[0].GetProperty("reason").GetString());
        Assert.Equal("imported", items[0].GetProperty("note").GetString());
        // Unset optional fields are omitted.
        Assert.Equal("not-an-address", items[1].GetProperty("email").GetString());
        Assert.False(items[1].TryGetProperty("topic", out _));
        Assert.False(items[1].TryGetProperty("reason", out _));
        Assert.False(items[1].TryGetProperty("note", out _));
    }
}
