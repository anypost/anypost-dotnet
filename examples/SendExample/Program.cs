using System;
using System.Threading.Tasks;
using Anypost;
using Anypost.Models;

// Reads the API key from ANYPOST_API_KEY. Point ANYPOST_BASE_URL at a dev gateway
// to smoke-test against a non-production environment.
var options = new AnypostClientOptions
{
    BaseUrl = Environment.GetEnvironmentVariable("ANYPOST_BASE_URL") ?? "https://api.anypost.com/v1",
};

using var client = AnypostClient.FromEnv(options);

try
{
    var sent = await client.Email.SendAsync(new SendEmailRequest
    {
        From = "Acme <hello@example.com>",
        To = ["alex@customer.com"],
        ReplyTo = ["support@example.com"],
        Subject = "Welcome to Acme",
        Html = "<p>Glad you are here.</p>",
        Text = "Glad you are here.",
        Tags = ["welcome"],
    });

    Console.WriteLine($"Accepted: {sent.Id} at {sent.CreatedAt}");
}
catch (AnypostException ex)
{
    Console.Error.WriteLine($"Send failed ({ex.Type}): {ex.Message}");
    if (ex.ValidationErrors is not null)
    {
        foreach (var (field, problems) in ex.ValidationErrors)
        {
            Console.Error.WriteLine($"  {field}: {string.Join("; ", problems)}");
        }
    }

    Environment.Exit(1);
}
