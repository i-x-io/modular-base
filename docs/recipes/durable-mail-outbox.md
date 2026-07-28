# Durable mail outbox with MimeKit and MailKit

## Problem and boundary

Use this recipe when a business transaction must record an email intent durably and a background worker submits that intent later. The application database owns the atomic business-write-plus-outbox-write transaction, claiming, leases, attempts, and terminal state. MimeKit owns MIME construction; MailKit owns SMTP connection, authentication, and submission. SMTP acceptance is not inbox delivery, and an interrupted submission can have an unknown outcome, so this design does not promise exactly-once delivery.

## Required catalog packages

The catalog supplies versions centrally. The following Worker SDK block is a
consuming-application example using those centrally managed versions:

```xml
<Project Sdk="Microsoft.NET.Sdk.Worker">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>14.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="MailKit" />
    <PackageReference Include="MimeKit" />
    <PackageReference Include="Microsoft.Extensions.Hosting" />
  </ItemGroup>
</Project>
```

`MimeKit` is referenced directly because this worker constructs MIME messages; `MailKit` performs transport. `Microsoft.Extensions.Hosting` supplies the worker lifecycle. The durable repository implementation belongs in the application's selected database project and must use the same transaction as the business change when it enqueues a message.

## Persist the intent, not a ready-to-send network operation

```csharp
public sealed record OutboxMail(
    Guid Id,
    string Recipient,
    string Subject,
    string TextBody,
    int AttemptCount,
    DateTimeOffset NextAttemptAt,
    string LeaseToken);

public interface IMailOutbox
{
    Task<IReadOnlyList<OutboxMail>> ClaimDueAsync(
        int maximumCount,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    Task MarkSubmissionStartedAsync(
        Guid id,
        string leaseToken,
        CancellationToken cancellationToken);

    Task MarkSubmittedAsync(
        Guid id,
        string leaseToken,
        CancellationToken cancellationToken);

    Task DeferAsync(
        Guid id,
        string leaseToken,
        DateTimeOffset nextAttemptAt,
        string failureCode,
        CancellationToken cancellationToken);

    Task MarkPermanentFailureAsync(
        Guid id,
        string leaseToken,
        string failureCode,
        CancellationToken cancellationToken);

    Task MarkOutcomeUnknownAsync(
        Guid id,
        string leaseToken,
        string failureCode,
        CancellationToken cancellationToken);
}
```

An enqueue operation inserts the business change and an immutable mail intent in one database transaction. `ClaimDueAsync` must atomically select due rows and assign a unique lease token so concurrent workers cannot submit the same claim; every update compares both the row ID and lease token. `MarkSubmissionStartedAsync` durably records the transition immediately before network submission. Lease recovery distinguishes a pre-submission claim from `SubmissionStarted`: an expired `SubmissionStarted` lease transitions to outcome-unknown and reconciliation, and **must not** be blindly resent. If `MarkSubmissionStartedAsync` fails, no network side effect occurred. Store a template identifier and bounded, non-secret render data instead of bodies when policy requires late rendering or reduced sensitive-data retention.

## Make transport outcomes explicit

```csharp
public enum DeliveryDisposition
{
    Retry,
    PermanentFailure,
    OutcomeUnknown
}

public sealed class MailDeliveryException(
    DeliveryDisposition disposition,
    string failureCode,
    Exception innerException)
    : Exception(failureCode, innerException)
{
    public DeliveryDisposition Disposition { get; } = disposition;
    public string FailureCode { get; } = failureCode;
}

public interface IMailTransport
{
    Task SubmitAsync(OutboxMail mail, CancellationToken cancellationToken);
}
```

The transport converts protocol details into three repository decisions. A failure before submission starts may be retried; an explicit permanent SMTP rejection is terminal; a connection loss or timeout during `SendAsync` is outcome-unknown and requires provider reconciliation or an operator decision before another send. Failure codes must be bounded categories, not exception messages, addresses, credentials, subjects, or message bodies.

## Compose and submit through MailKit

```csharp
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

public sealed record SmtpSettings(
    string Host,
    int Port,
    SecureSocketOptions SocketOptions,
    string UserName,
    string Password,
    string SenderName,
    string SenderAddress,
    string MessageIdDomain);

public sealed class MailKitTransport(SmtpSettings settings) : IMailTransport
{
    public async Task SubmitAsync(
        OutboxMail mail,
        CancellationToken cancellationToken)
    {
        MimeMessage message;
        try
        {
            message = new MimeMessage
            {
                Subject = mail.Subject,
                MessageId = $"{mail.Id:N}@{settings.MessageIdDomain}"
            };
            message.From.Add(new MailboxAddress(
                settings.SenderName,
                settings.SenderAddress));
            message.To.Add(MailboxAddress.Parse(mail.Recipient));
            message.Body = new TextPart("plain") { Text = mail.TextBody };
        }
        catch (FormatException exception)
        {
            throw new MailDeliveryException(
                DeliveryDisposition.PermanentFailure,
                "invalid_message",
                exception);
        }

        using var client = new SmtpClient { Timeout = 30_000 };

        try
        {
            SecureSocketOptions socketOptions = RequireTransportSecurity(
                settings.SocketOptions);
            await client.ConnectAsync(
                settings.Host,
                settings.Port,
                socketOptions,
                cancellationToken);
            await client.AuthenticateAsync(
                settings.UserName,
                settings.Password,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (AuthenticationException exception)
        {
            throw new MailDeliveryException(
                DeliveryDisposition.PermanentFailure,
                "smtp_authentication",
                exception);
        }
        catch (ServiceNotConnectedException exception)
        {
            throw new MailDeliveryException(
                DeliveryDisposition.Retry,
                "smtp_connect_or_auth",
                exception);
        }
        catch (ServiceNotAuthenticatedException exception)
        {
            throw new MailDeliveryException(
                DeliveryDisposition.Retry,
                "smtp_connect_or_auth",
                exception);
        }
        catch (IOException exception)
        {
            throw new MailDeliveryException(
                DeliveryDisposition.Retry,
                "smtp_connect_or_auth",
                exception);
        }

        try
        {
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(quit: true, cancellationToken);
        }
        catch (SmtpCommandException exception) when ((int)exception.StatusCode >= 500)
        {
            throw new MailDeliveryException(
                DeliveryDisposition.PermanentFailure,
                $"smtp_{(int)exception.StatusCode}",
                exception);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SmtpCommandException exception)
        {
            throw new MailDeliveryException(
                DeliveryDisposition.OutcomeUnknown,
                "smtp_submission_interrupted",
                exception);
        }
        catch (SmtpProtocolException exception)
        {
            throw new MailDeliveryException(
                DeliveryDisposition.OutcomeUnknown,
                "smtp_submission_interrupted",
                exception);
        }
        catch (IOException exception)
        {
            throw new MailDeliveryException(
                DeliveryDisposition.OutcomeUnknown,
                "smtp_submission_interrupted",
                exception);
        }
    }

    private static SecureSocketOptions RequireTransportSecurity(
        SecureSocketOptions socketOptions) => socketOptions switch
    {
        SecureSocketOptions.StartTls or SecureSocketOptions.SslOnConnect => socketOptions,
        _ => throw new InvalidOperationException(
            "SMTP requires SecureSocketOptions.StartTls or SslOnConnect.")
    };
}
```

MimeKit constructs structured addresses, a deterministic RFC message ID derived from the durable outbox ID, and the MIME body before any network call. Invalid address data becomes a permanent record failure instead of terminating the worker. Before it opens a socket, `RequireTransportSecurity` permits only mandatory `StartTls` or implicit-TLS `SslOnConnect`; it rejects `None`, `Auto`, and `StartTlsWhenAvailable`, so a server cannot downgrade this worker to plaintext. MailKit then connects, authenticates, submits, and disconnects in that order. The SMTP response is discarded: it is transport detail, not durable outbox data. The example creates one client per item for a clear ownership boundary; a production worker may reuse one connected client for a bounded sequential batch, but must discard it after protocol or I/O failure and must never use one `SmtpClient` concurrently. Obtain credentials from an approved secret provider, prefer provider-supported OAuth2 where required, keep normal certificate validation enabled, and validate all settings at startup.

Transport cancellation is never translated. The worker also treats cancellation after SMTP acceptance but before `MarkSubmittedAsync` completes as outcome-unknown. In either case it attempts a bounded independent finalization before rethrowing the original cancellation, because the provider may have accepted the message without a durable submitted record. It rethrows the original cancellation only if finalization succeeds; if finalization fails, that real persistence exception escapes rather than being swallowed. Refine permanent-versus-transient SMTP classification against the chosen provider's documented status codes. A stable `Message-Id` helps reconciliation but does not make arbitrary SMTP providers idempotent.

## Run the leased worker

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public sealed class MailOutboxWorker(
    IMailOutbox outbox,
    IMailTransport transport,
    TimeProvider clock,
    ILogger<MailOutboxWorker> logger) : BackgroundService
{
    private static readonly TimeSpan ShutdownFinalizationBudget =
        TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2), clock);

        do
        {
            IReadOnlyList<OutboxMail> batch = await outbox.ClaimDueAsync(
                maximumCount: 20,
                leaseDuration: TimeSpan.FromMinutes(2),
                stoppingToken);

            foreach (OutboxMail mail in batch)
            {
                try
                {
                    await outbox.MarkSubmissionStartedAsync(
                        mail.Id, mail.LeaseToken, stoppingToken);
                    await transport.SubmitAsync(mail, stoppingToken);
                    await outbox.MarkSubmittedAsync(
                        mail.Id, mail.LeaseToken, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    await FinalizeUnknownOutcomeAsync(mail);
                    throw;
                }
                catch (MailDeliveryException exception)
                {
                    await RecordFailureAsync(mail, exception, stoppingToken);
                    logger.LogWarning(
                        "Mail outbox item {OutboxId} ended with {Disposition} ({FailureCode})",
                        mail.Id,
                        exception.Disposition,
                        exception.FailureCode);
                    continue;
                }
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RecordFailureAsync(
        OutboxMail mail,
        MailDeliveryException exception,
        CancellationToken cancellationToken)
    {
        switch (exception.Disposition)
        {
            case DeliveryDisposition.Retry:
                await outbox.DeferAsync(
                    mail.Id,
                    mail.LeaseToken,
                    clock.GetUtcNow() + RetryDelay(mail.AttemptCount),
                    exception.FailureCode,
                    cancellationToken);
                return;

            case DeliveryDisposition.PermanentFailure:
                await outbox.MarkPermanentFailureAsync(
                    mail.Id,
                    mail.LeaseToken,
                    exception.FailureCode,
                    cancellationToken);
                return;

            case DeliveryDisposition.OutcomeUnknown:
                if (cancellationToken.IsCancellationRequested)
                {
                    await FinalizeUnknownOutcomeAsync(mail, exception.FailureCode);
                    return;
                }

                try
                {
                    await outbox.MarkOutcomeUnknownAsync(
                        mail.Id,
                        mail.LeaseToken,
                        exception.FailureCode,
                        cancellationToken);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    await FinalizeUnknownOutcomeAsync(mail, exception.FailureCode);
                    throw;
                }
                return;

            default:
                throw new ArgumentOutOfRangeException(nameof(exception));
        }
    }

    private async Task FinalizeUnknownOutcomeAsync(
        OutboxMail mail,
        string failureCode = "smtp_submission_cancelled")
    {
        // The send may have reached SMTP. Persist that uncertainty even though
        // normal worker work is stopping; this budget is independent.
        using var finalization = new CancellationTokenSource(
            ShutdownFinalizationBudget);
        await outbox.MarkOutcomeUnknownAsync(
            mail.Id,
            mail.LeaseToken,
            failureCode,
            finalization.Token);
    }

    private static TimeSpan RetryDelay(int attempts) =>
        TimeSpan.FromSeconds(Math.Min(300, 5 * Math.Pow(2, attempts)));
}
```

The worker claims before it sends and records `SubmissionStarted` while it still owns the lease. Only after that transition succeeds does it contact SMTP. Backoff is bounded; the repository must also cap attempts and move exhausted records to an observable terminal state. Host cancellation stops new work, while an interrupted in-flight submission follows the unknown-outcome path. When cancellation has already interrupted the normal unknown-outcome write, the worker retries that one finalization with an independent five-second budget; it is deliberately not linked to `stoppingToken`, because that token is already cancelled. It rethrows the original cancellation after a successful finalization. If that finalization fails, its persistence exception remains visible. A failure of `MarkSubmittedAsync` after SMTP acceptance also remains a visible real exception; recovery sees `SubmissionStarted` and reconciles rather than retrying blindly. Even then, a hard crash after provider acceptance but before the database update can cause a later duplicate; reconcile provider events where available and make the business impact duplicate-tolerant. In production, add lease renewal when a bounded batch can exceed the lease, add jitter to retries, and make repository writes resilient without losing the lease compare-and-set invariant.

## Failure modes and operations

| Signal or symptom | Interpretation | Action |
| --- | --- | --- |
| Oldest pending age and due depth rise | Worker, provider, or database is unavailable or throughput is insufficient | Alert on sustained age; inspect bounded failure codes and dependency telemetry |
| Lease-expiration count rises | Workers crash, hang, or exceed the lease | Correlate shutdowns and duration; adjust batch/lease only after fixing stalls |
| `SubmissionStarted` lease expires or `MarkSubmittedAsync` fails | SMTP may have accepted the message but the submitted update is absent or ambiguous | Transition to outcome-unknown, reconcile provider events and `Message-Id`, and do not resend blindly |
| `OutcomeUnknown` records appear | Submission began but acknowledgement was not observed | Reconcile using provider events and `Message-Id`; do not auto-resend blindly |
| Permanent rejection rate rises | Recipient/policy/data error or provider contract change | Quarantine, correct the source, and avoid retry loops |
| Authentication or TLS failures rise | Credential, clock, trust, port, or provider-policy problem | Rotate/fix configuration; never bypass certificate validation |

Record stable outbox IDs, attempt counts, queue age, phase durations, SMTP status class, and disposition. Do not record credentials, auth tokens, full SMTP responses, recipient addresses unless policy permits, subjects, bodies, or attachments. Provider acceptance and bounce/complaint events need separate operational handling.

## Verification checklist

Authoring verification for this recipe:

- [x] The worker example was compiled in a temporary `net10.0` `Microsoft.NET.Sdk.Worker` project with the catalog package versions.
- [x] No SMTP connection or real message submission was performed during authoring.

Checks for the consuming application:

- [ ] Prove the business write and outbox insert commit or roll back atomically.
- [ ] Run two workers and prove lease-token compare-and-set prevents concurrent submission.
- [ ] Exercise crash-before-send, rejection, timeout during send, crash-after-acceptance, lease expiry, retry exhaustion, and graceful shutdown.
- [ ] Assert `MailKitTransport` rejects `None`, `Auto`, and `StartTlsWhenAvailable`, and accepts only `StartTls` or `SslOnConnect` before `ConnectAsync`.
- [ ] Verify submitted records retain no raw SMTP response or other server response text.
- [ ] Cancel an in-flight send, cancel `MarkSubmittedAsync` after a successful `SendAsync`, and cancel a normal `MarkOutcomeUnknownAsync` write, then verify each bounded finalization uses an independent token; successful finalization rethrows the original cancellation, while an expired finalization budget remains observable.
- [ ] Force `MarkSubmissionStartedAsync` to fail and verify no SMTP call occurs; force `MarkSubmittedAsync` to fail after SMTP acceptance and verify recovery records/reconciles the expired `SubmissionStarted` lease without blindly resending.
- [ ] Verify reconciliation and operator handling for unknown outcomes, bounces, and complaints.
- [ ] Test the approved provider's TLS/authentication contract and confirm telemetry redaction.

## Related package guides

- [MailKit](../packages/mailkit.md)
- [MimeKit](../packages/mimekit.md)
- [Microsoft.Extensions.Hosting](../packages/microsoft-extensions-hosting.md)
- [Microsoft.Extensions.Options](../packages/microsoft-extensions-options.md)

## Primary sources

- [MailKit 4.17.0 on NuGet](https://www.nuget.org/packages/MailKit/4.17.0) — Accessed 2026-07-27.
- [MailKit 4.17.0 SMTP examples](https://github.com/jstedfast/MailKit/blob/4.17.0/Documentation/Examples/SmtpExamples.cs) — Accessed 2026-07-27.
- [MailKit `SmtpClient.SendAsync`](https://mimekit.net/docs/html/M_MailKit_Net_Smtp_SmtpClient_SendAsync_2.htm) — Accessed 2026-07-27.
- [MimeKit message-building FAQ](https://github.com/jstedfast/MimeKit/blob/4.17.0/FAQ.md) — Accessed 2026-07-27.
