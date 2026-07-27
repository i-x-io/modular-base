# MailKit

## Catalog entry

`MailKit` **4.17.0** — direct catalog package; SMTP, IMAP, and POP client library. It works with the MIME model provided by MimeKit.

## Decision and scope

Use for protocol-level mail send/receive where the application owns connection, authentication, and delivery semantics. It does not provide an outbox, queue, template engine, or delivery guarantee.

## Recommended registration and use

The catalog supplies the version centrally, so the transport project keeps the reference versionless:

```xml
<ItemGroup>
  <PackageReference Include="MailKit" />
</ItemGroup>
```

Create the `MimeMessage` with MimeKit first. MailKit then owns the asynchronous transport lifecycle:

```csharp
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

static async Task<string> SendAsync(
    MimeMessage message,
    string userName,
    string password,
    CancellationToken cancellationToken)
{
    using var client = new SmtpClient();
    await client.ConnectAsync(
        "smtp.example.com",
        587,
        SecureSocketOptions.StartTls,
        cancellationToken);
    await client.AuthenticateAsync(userName, password, cancellationToken);

    var response = await client.SendAsync(message, cancellationToken);
    await client.DisconnectAsync(quit: true, cancellationToken);
    return response;
}
```

Choose the host, port, and `SecureSocketOptions` from the provider contract. Use `SslOnConnect` for implicit TLS where required; `StartTls` fails if STARTTLS is unavailable, which is generally safer than opportunistic downgrade behavior.

## Enterprise implementation guidance

The common delivery workflow is: claim a durable outbox record, compose the message, connect/authenticate, send, record the SMTP response, disconnect, and mark the record submitted. Reuse one connected, authenticated client for a bounded batch in a single worker to avoid repeated handshakes, but do not use that instance concurrently. On `SmtpCommandException`, inspect the status/error category before deciding whether to retry; after a protocol or I/O failure, discard the client. SMTP acceptance is not proof of inbox delivery, so correlate bounces and provider events with the outbox/message identifier.

## Integration with the catalog

`mimekit.md` owns message construction. Apply `polly.md` or `microsoft-extensions-resilience.md` around transient transport failures only, with bounded retries and no duplicate delivery assumption.

## Security, performance, AOT, trimming, and operations

Use TLS with normal certificate validation; do not bypass certificate errors. Prefer OAuth2/SASL mechanisms when the provider requires them, rotate secrets through approved storage, and never log credentials or full message bodies. Bound connection, command, message, and attachment sizes; propagate cancellation from worker shutdown. Instrument latency, response categories, reconnects, queue age, and retry/dead-letter outcomes without including recipient addresses unless policy permits. Validate publishing modes in the deployed target; no catalog AOT guarantee is asserted.

## Avoid

Do not use insecure transport in production, retry non-idempotent sends without an outbox strategy, or share one client concurrently across unrelated operations without the package's concurrency contract.

## Verification checklist

- [ ] Test TLS/authentication against the approved provider or a controlled test server.
- [ ] Verify a failed or ambiguous send leaves an observable, deduplicated outbox state.
- [ ] Test cancellation, timeout, reconnect, provider throttling, and permanent rejection paths.
- [ ] Confirm telemetry excludes credentials, bodies, and disallowed recipient data.

## Sources

- [NuGet Gallery: MailKit 4.17.0](https://www.nuget.org/packages/MailKit/4.17.0) (Accessed 2026-07-27)
- [MailKit 4.17.0 API: `SmtpClient`](https://mimekit.net/docs/html/T_MailKit_Net_Smtp_SmtpClient.htm) (Accessed 2026-07-27)
- [MailKit 4.17.0 API: asynchronous SMTP connection](https://mimekit.net/docs/html/M_MailKit_Net_Smtp_SmtpClient_ConnectAsync_2.htm) (Accessed 2026-07-27)
- [MailKit upstream SMTP examples](https://github.com/jstedfast/MailKit/blob/4.17.0/Documentation/Examples/SmtpExamples.cs) (Accessed 2026-07-27)
