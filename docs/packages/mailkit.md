# MailKit

## Catalog entry

`MailKit` **4.17.0** — direct catalog package; SMTP, IMAP, and POP client library. It works with the MIME model provided by MimeKit.

## Decision and scope

Use for protocol-level mail send/receive where the application owns connection, authentication, and delivery semantics. It does not provide an outbox, queue, template engine, or delivery guarantee.

## Recommended registration and use

Create and populate the message with MimeKit, then connect with an explicit secure-socket policy, authenticate, send, disconnect, and dispose. Use asynchronous operations in service paths; reuse an authenticated connection only inside a controlled worker/client lifetime.

## Enterprise implementation guidance

Send from a durable outbox/worker, assign message identifiers, capture server responses, and make retry behavior idempotency-aware. Store credentials in approved secret storage and choose TLS policy from provider documentation, not implicit defaults.

## Integration with the catalog

`mimekit.md` owns message construction. Apply `polly.md` or `microsoft-extensions-resilience.md` around transient transport failures only, with bounded retries and no duplicate delivery assumption.

## Security, performance, AOT, trimming, and operations

Use TLS and validate certificates; never log credentials or full message bodies by default. SMTP acceptance is not recipient delivery. Limit attachment/message sizes and instrument latency, response codes, and retry/delivery outcomes. Validate publishing modes in the deployed target; no catalog AOT guarantee is asserted.

## Avoid

Do not use insecure transport in production, retry non-idempotent sends without an outbox strategy, or share one client concurrently across unrelated operations without the package's concurrency contract.

## Verification checklist

- Test TLS/authentication against the approved mail provider or a controlled test server.
- Verify a failed send leaves an idempotent, observable outbox state.
- Test cancellation, timeout, attachment limits, and safe telemetry.

## Sources

- https://www.nuget.org/packages/MailKit/4.17.0 (Accessed 2026-07-27)
- https://github.com/jstedfast/MailKit (Accessed 2026-07-27)
- https://github.com/jstedfast/MailKit/blob/master/FAQ.md (Accessed 2026-07-27)
