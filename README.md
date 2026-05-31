# Anypost .NET SDK

The official .NET client for the [Anypost](https://anypost.com) email API.

Requires .NET 8+. JSON uses the in-box `System.Text.Json`; the only package
dependencies are the first-party `Microsoft.Extensions.Http` and
`Microsoft.Extensions.Options`, for the dependency-injection integration below.
The client is safe for concurrent use.

## Install

```bash
dotnet add package Anypost
```

## Quickstart

```csharp
using Anypost;
using Anypost.Models;

var client = AnypostClient.Create("ap_your_api_key");

var sent = await client.Email.SendAsync(new SendEmailRequest
{
    From = "Acme <you@yourdomain.com>",
    To = ["someone@example.com"],
    Subject = "Hello from Anypost",
    Html = "<p>It worked.</p>",
});

Console.WriteLine(sent.Id);
```

`AnypostClient.FromEnv()` reads the key from `ANYPOST_API_KEY` instead. Keep the
key server-side; it is a bearer credential.

## Dependency injection

In an ASP.NET Core or Worker host, register the client with `AddAnypost` and
inject `IAnypostClient`. This wires an `IHttpClientFactory`-managed typed client,
so the underlying `HttpClient` is pooled and its handler lifetime is managed for
you — no socket exhaustion from per-request clients.

```csharp
using Anypost;

builder.Services.AddAnypost(o =>
{
    o.ApiKey = builder.Configuration["Anypost:ApiKey"];
    // o.MaxRetries = 3; // any AnypostClientOptions field
});
```

```csharp
public sealed class WelcomeMailer(IAnypostClient anypost)
{
    public Task SendAsync(string to) =>
        anypost.Email.SendAsync(new SendEmailRequest
        {
            From = "Acme <you@yourdomain.com>",
            To = [to],
            Subject = "Welcome",
            Html = "<p>Glad you are here.</p>",
        });
}
```

`AddAnypost` returns the `IHttpClientBuilder`, so you can layer on transport
configuration (a custom primary handler, Polly resilience policies, and so on).
With no configuration, `AddAnypost()` reads the key from `ANYPOST_API_KEY`.
Outside a host — console apps, scripts, tests — construct the client directly
with `AnypostClient.Create(...)`; no container required.

## Sending

One of `Text`, `Html`, or `TemplateId` is required. All addresses in `To`, `Cc`,
and `Bcc` share one envelope and count against a combined limit of 50.

```csharp
await client.Email.SendAsync(new SendEmailRequest
{
    From = "Acme <you@yourdomain.com>",
    To = ["a@example.com", "b@example.com"],
    Cc = ["team@example.com"],
    ReplyTo = ["support@yourdomain.com"],
    Subject = "Receipt #4823",
    Html = "<p>Thanks for your order.</p>",
    Text = "Thanks for your order.",
    Tags = ["receipt"],
});
```

`Attachment` content is the raw file bytes — pass what `File.ReadAllBytes`
returns and the SDK base64-encodes it on the wire. Do not pre-encode it. The
request body is capped at 5 MB.

```csharp
byte[] pdf = File.ReadAllBytes("report.pdf");

await client.Email.SendAsync(new SendEmailRequest
{
    From = "you@yourdomain.com",
    To = ["someone@example.com"],
    Subject = "Your report",
    Text = "Attached.",
    Attachments = [Attachment.Create("report.pdf", pdf, "application/pdf")],
});
```

Send with a published template and per-recipient variables:

```csharp
await client.Email.SendAsync(new SendEmailRequest
{
    From = "you@yourdomain.com",
    To = ["someone@example.com"],
    TemplateId = "template_018f2c5e-3a40-7a91-9c25-3a0b1d5e6f78",
    Variables = new Dictionary<string, object?> { ["name"] = "Ada", ["plan"] = "pro" },
});
```

## Batch

Send 1 to 100 independent messages in one request. `Defaults` fills any field an
entry omits. Leave an entry's `From` (and any other shared field) unset to
inherit the default; an entry that sets its own value wins. `To` is always
per-entry.

```csharp
var result = await client.Email.SendBatchAsync(new EmailBatchRequest
{
    Defaults = new SendEmailRequest { From = "you@yourdomain.com" },
    Emails =
    [
        new SendEmailRequest { To = ["a@example.com"], Subject = "Hi A", Text = "..." },
        new SendEmailRequest { To = ["b@example.com"], Subject = "Hi B", Text = "..." },
    ],
});
```

A batch with mixed outcomes returns HTTP `207` and does not throw. Inspect each
entry's status rather than treating it as a failure:

```csharp
Console.WriteLine($"{result.Summary.Queued}/{result.Summary.Total}");

foreach (var entry in result.Data)
{
    if (entry.IsQueued)
    {
        Console.WriteLine($"{entry.Index} {entry.Id}");
    }
    else
    {
        Console.WriteLine($"{entry.Index} {entry.Error!.Type} {entry.Error.Message}");
    }
}
```

## Domains

Manage sending domains under `client.Domains`. Add a domain, publish the records
it returns, then verify.

```csharp
var domain = await client.Domains.CreateAsync(new DomainCreateParams { Name = "example.com" });
foreach (var record in domain.DnsRecords)
{
    Console.WriteLine($"{record.Type} {record.Name} -> {record.Value}");
}
```

`VerifyAsync` always returns the current domain — a still-`pending` domain is not
an error. Read its status and verification failure, and poll while DNS
propagates.

```csharp
var refreshed = await client.Domains.VerifyAsync(domain.Id);
if (refreshed.Status != "verified" && refreshed.VerificationFailure is not null)
{
    Console.WriteLine(refreshed.VerificationFailure.Code);
}
```

`GetAsync`, `UpdateAsync` (tracking config only), and `DeleteAsync` round out the
resource.

## API keys

Manage keys under `client.ApiKeys`. The plaintext secret comes back only once, on
`CreateAsync`, as `Key`:

```csharp
var created = await client.ApiKeys.CreateAsync(new ApiKeyCreateParams
{
    Name = "Production server",
    Permissions = Permissions.SendOnly,
    AllowedDomains = ["example.com"],
});

Console.WriteLine(created.Key); // store now; never retrievable again
```

`GetAsync` returns metadata only — `KeyPrefix`, never the secret. Permission and
restriction changes take up to 5 minutes to propagate through the gateway cache.

## Templates

Templates use a draft/published model: edits land in a draft, and `PublishAsync`
promotes it. A template can't be used for sending until it's published.

```csharp
var tmpl = await client.Templates.CreateAsync(new TemplateCreateParams
{
    Name = "Welcome email",
    Kind = TemplateKind.Html,
    Html = "<h1>Welcome, {{ name }}</h1>",
});

await client.Templates.UpdateDraftAsync(tmpl.Id, new TemplateDraftParams
{
    Subject = "Welcome to Acme",
    Html = "<h1>Welcome, {{ name }}</h1>",
});

await client.Templates.PublishAsync(tmpl.Id);
```

`Kind` is `Html` or `Markdown` and is immutable once set. `GetDraftAsync`,
`DeleteDraftAsync`, `DuplicateAsync`, `GetAsync`, `UpdateAsync` (name only), and
`DeleteAsync` round out the resource. Send with a published template via
`TemplateId` (see [Sending](#sending)).

## Suppressions

A suppression blocks sends to an address, scoped to a `Topic`. The wildcard `*`
blocks every topic; a specific topic (e.g. `marketing`) leaves transactional
traffic untouched. Bounces and complaints write `*` automatically.

```csharp
await client.Suppressions.CreateAsync(new SuppressionCreateParams
{
    Email = "alice@example.com",
    Topic = "marketing",
    Note = "Customer requested removal",
});

var row = await client.Suppressions.GetAsync("alice@example.com", "*");
await client.Suppressions.DeleteAsync("alice@example.com", "marketing");

var complaints = await client.Suppressions.ListAsync(new SuppressionListParams
{
    Reason = SuppressionReason.Complaint,
});
```

`ListForEmailAsync` returns every row for an address across all topics;
`DeleteForEmailAsync` removes them all.

## Webhooks

Manage webhook subscriptions under `client.Webhooks`. The signing secret comes
back only once, on `CreateAsync`; later reads return only the prefix.

```csharp
var wh = await client.Webhooks.CreateAsync(new WebhookCreateParams
{
    Name = "Production events",
    Url = "https://hooks.example.com/anypost",
    Events = [WebhookEventType.Delivered, WebhookEventType.Bounced, WebhookEventType.Complained],
});

Console.WriteLine(wh.SigningSecret); // store now; never retrievable again
```

`UpdateAsync` sets the name, URL, events, and status together — set the status to
`WebhookStatus.Disabled` to pause delivery, `Active` to resume. `TestAsync` sends
one synthetic `webhook.test` event and returns the outcome even when the endpoint
fails. `RotateSecretAsync` issues a new secret and keeps the previous one valid
for a 24-hour grace window; `GetAsync`, `ListAsync`, and `DeleteAsync` round out
the resource.

### Verifying deliveries

`WebhookVerifier` has static methods — they need the signing secret, not an API
key, so call them in your handler without a client. Pass the **raw** request body
(the exact bytes, before JSON parsing), the `Anypost-Signature` header, and the
secret. `Unwrap` verifies and returns the parsed delivery in one step:

```csharp
try
{
    var delivery = WebhookVerifier.Unwrap(rawBody, signatureHeader, signingSecret);
    foreach (var ev in delivery.Events)
    {
        // ev.Type, ev.Data?["email_id"], ...
    }
}
catch (WebhookVerificationException ex)
{
    // ex.Reason: NoMatch, TimestampOutOfTolerance, ...
    return Results.StatusCode(400);
}
```

Reach for `WebhookVerifier.VerifySignature(...)` when something else has already
parsed the body — keep the raw bytes for the verify step, then use your parsed
value once it passes. Deliveries older than five minutes are rejected by default
to bound replay; `WebhookVerifyOptions` widens, narrows, or disables
(`TimeSpan.Zero`) that check, and overrides the clock in tests. During a secret
rotation the header carries a `v1=` component per active secret, and a match on
any one passes, so deliveries keep verifying while you redeploy.

## Events

`client.Events.ListAsync` pages the team's event stream, newest-first. The window
defaults to the last 24 hours and is clamped to your plan's retention. Events are
read-only and not addressable by id — there is no `Get`.

```csharp
var page = await client.Events.ListAsync(new EventListParams { EventType = EventType.Bounced });

foreach (var ev in page.Data)
{
    Console.WriteLine($"{ev.OccurredAt} {ev.Recipient} {ev.BounceClassification}");
}
```

Filter by `Start`, `End`, `EventType`, `Recipient`, `EmailId`, `MessageId`,
`Domain`, `Topic`, `Campaign`, `TemplateId`, and `Tags`. All filters are
exact-match, except `Tags`, which matches an event carrying *any* of the given
tags. This is also how you backfill the gap after a webhook endpoint was disabled
— page the events that occurred during the outage once it's healthy.

## Pagination

List endpoints return a `Page<T>` with `Data`, `HasMore`, and `NextCursor`. Read
one page, call `NextAsync` to fetch the following one, or `await foreach` over
`AllAsync` to walk every item across pages, re-fetching as it goes.

```csharp
var page = await client.Domains.ListAsync(new ListParams { Limit = 50 });
page.Data;        // this page's items
page.HasMore;     // whether another page exists
page.NextCursor;  // pass to ListParams.After to fetch it yourself

await foreach (var domain in (await client.Domains.ListAsync()).AllAsync())
{
    Console.WriteLine(domain.Name); // every domain, across all pages
}
```

## Errors

A failed request throws an `AnypostException`. Branch on `Type`, the stable,
machine-readable `error.type` — not on the HTTP status.

```csharp
try
{
    await client.Email.SendAsync(message);
}
catch (AnypostException ex)
{
    switch (ex.Type)
    {
        case ErrorType.Validation:
            Console.WriteLine(ex.ValidationErrors); // field -> messages
            break;
        case ErrorType.RateLimit:
            Console.WriteLine(ex.RetryAfter);        // TimeSpan?, may be null
            break;
        default:
            Console.WriteLine($"{ex.Type} {ex.StatusCode} {ex.RequestId}");
            break;
    }
}
```

| `ErrorType` | `error.type` | Status |
|---|---|---|
| `Validation` | `validation_error` | `400`, `422` |
| `Authentication` | `authentication_error` | `401` |
| `Permission` | `permission_error` | `403` |
| `NotFound` | `not_found` | `404` |
| `Conflict` / `IdempotencyConflict` / `WebhookRotationInProgress` | `conflict`, `idempotency_concurrent`, `webhook_rotation_in_progress` | `409` |
| `IdempotencyMismatch` | `idempotency_mismatch` | `422` |
| `RateLimit` | `rate_limit_exceeded` | `429` |
| `PayloadTooLarge` | `payload_too_large` | `413` |
| `Internal` / `Provisioning` | `internal_error`, `provisioning_error` | `5xx` |
| `ApiError` | (unrecognized type) | any |
| `Connection` | (no response) | none |

Every API-level error carries `Type`, `StatusCode`, `RequestId`, the message, and
the raw `Body`. A connection error (no response) carries `ErrorType.Connection`, a
null `StatusCode`, and the underlying transport error via `InnerException`.

## Retries and idempotency

The client retries `429`, `502`, `503`, and network failures up to `MaxRetries`
times (default 2), with exponential backoff and full jitter. It honors
`Retry-After`.

Sends are made safe to retry automatically: when retries are enabled and you do
not pass an idempotency key, the client generates one and reuses it across
attempts, so a retried send cannot deliver twice. Pass your own key to dedupe
across process restarts:

```csharp
await client.Email.SendAsync(message, new RequestOptions { IdempotencyKey = "order-4823" });
```

## Configuration

```csharp
var client = AnypostClient.Create("ap_your_api_key", new AnypostClientOptions
{
    BaseUrl = "https://api.anypost.com/v1",
    Timeout = TimeSpan.FromSeconds(30),
    MaxRetries = 2,
    HttpClient = myHttpClient,        // configure a proxy or custom transport
    DefaultHeaders = new Dictionary<string, string> { ["X-App"] = "billing" },
});
```

| Option | Default | Description |
|---|---|---|
| `ApiKey` | `ANYPOST_API_KEY` | Mainly for the DI path; the direct constructor takes the key as its argument. |
| `BaseUrl` | `https://api.anypost.com/v1` | API base URL. |
| `Timeout` | 30s | Per-request timeout; `TimeSpan.Zero` disables it. |
| `MaxRetries` | 2 | Automatic retries for transient failures. |
| `HttpClient` | a fresh one | Custom client/transport (proxy, TLS); you own its lifetime. |
| `DefaultHeaders` | none | Headers sent on every request. |

`AnypostClient.FromEnv()` reads `ANYPOST_API_KEY` from the environment. When the
client creates its own `HttpClient`, dispose the client (or use a `using`) to
release it; a client you pass in is yours to dispose.

## License

MIT
