using System;
using System.Net;
using System.Threading.Tasks;
using Anypost;
using Anypost.Models;
using Xunit;

namespace Anypost.Tests;

public class ClientTests
{
    [Fact]
    public void Constructor_throws_when_no_key_and_env_unset()
    {
        var previous = Environment.GetEnvironmentVariable("ANYPOST_API_KEY");
        Environment.SetEnvironmentVariable("ANYPOST_API_KEY", null);
        try
        {
            Assert.Throws<ArgumentException>(() => new AnypostClient(""));
        }
        finally
        {
            Environment.SetEnvironmentVariable("ANYPOST_API_KEY", previous);
        }
    }

    [Fact]
    public void FromEnv_reads_the_environment_variable()
    {
        var previous = Environment.GetEnvironmentVariable("ANYPOST_API_KEY");
        Environment.SetEnvironmentVariable("ANYPOST_API_KEY", "ap_from_env");
        try
        {
            using var client = AnypostClient.FromEnv();
            Assert.NotNull(client);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ANYPOST_API_KEY", previous);
        }
    }

    [Fact]
    public async Task Assembles_the_expected_default_headers()
    {
        using var client = TestClient.Create(out var handler, out _);
        handler.Enqueue(HttpStatusCode.OK, """{"team":{"id":"team_1","name":"Acme"},"api_key":{"id":"key_1","permissions":"full"}}""");

        await client.WhoamiAsync();

        var request = Assert.Single(handler.Requests);
        Assert.Equal("Bearer ap_test", request.Header("Authorization"));
        Assert.Equal("application/json", request.Header("Accept"));
        Assert.StartsWith("anypost-dotnet/", request.Header("User-Agent"));
        Assert.Equal("https://api.test/v1/whoami", request.RequestUri.ToString());
    }

    [Fact]
    public async Task Whoami_parses_team_and_key()
    {
        using var client = TestClient.Create(out var handler, out _);
        handler.Enqueue(HttpStatusCode.OK, """{"team":{"id":"team_1","name":"Acme"},"api_key":{"id":"key_1","permissions":"send_only"}}""");

        var me = await client.WhoamiAsync();

        Assert.Equal("team_1", me.Team!.Id);
        Assert.Equal("Acme", me.Team.Name);
        Assert.Equal("key_1", me.ApiKey.Id);
        Assert.Equal(Permissions.SendOnly, me.ApiKey.Permissions);
    }

    [Fact]
    public async Task Unknown_enum_value_maps_to_Unknown()
    {
        using var client = TestClient.Create(out var handler, out _);
        handler.Enqueue(HttpStatusCode.OK, """{"team":null,"api_key":{"id":"key_1","permissions":"superuser"}}""");

        var me = await client.WhoamiAsync();

        Assert.Null(me.Team);
        Assert.Equal(Permissions.Unknown, me.ApiKey.Permissions);
    }
}
