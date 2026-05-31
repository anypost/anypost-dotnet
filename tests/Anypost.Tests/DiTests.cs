using System.Net;
using System.Threading.Tasks;
using Anypost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Anypost.Tests;

public class DiTests
{
    [Fact]
    public async Task AddAnypost_resolves_a_working_typed_client()
    {
        var handler = new MockHandler();
        handler.Enqueue(HttpStatusCode.OK, """{"team":{"id":"team_1","name":"Acme"},"api_key":{"id":"key_1","permissions":"full"}}""");

        var services = new ServiceCollection();
        services.AddAnypost(o => o.ApiKey = "ap_di_test")
                .ConfigurePrimaryHttpMessageHandler(() => handler);

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IAnypostClient>();

        var me = await client.WhoamiAsync();

        Assert.Equal("team_1", me.Team!.Id);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("Bearer ap_di_test", request.Header("Authorization"));
        Assert.Equal("https://api.anypost.com/v1/whoami", request.RequestUri.ToString());
    }

    [Fact]
    public void AddAnypost_returns_an_http_client_builder_for_chaining()
    {
        var services = new ServiceCollection();
        IHttpClientBuilder builder = services.AddAnypost(o => o.ApiKey = "ap_x");
        Assert.NotNull(builder);
    }
}
