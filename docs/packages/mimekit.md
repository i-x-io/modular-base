# MimeKit

## Catalog entry

`MimeKit` **4.17.0** — direct catalog package; MIME message, body, address, header, and attachment construction/parsing library.

## Decision and scope

Use to model messages and attachments correctly before handing them to MailKit. It does not send or receive mail transport traffic.

## Recommended registration and use

Build a `MimeMessage` with structured addresses, subject, and body parts; use `BodyBuilder` for ordinary text/HTML plus attachments. Preserve the original parsed message when fidelity is required rather than reconstructing headers from strings.

## Enterprise implementation guidance

Separate template rendering from MIME construction, set content types deliberately, impose attachment and decoded-content size limits, and keep message identifiers and audit metadata outside the user-visible body. Validate recipients before message creation.

## Integration with the catalog

`mailkit.md` delivers the resulting message. `anglesharp.md` may process HTML templates or inbound HTML, but parsing does not sanitize it for display.

## Security, performance, AOT, trimming, and operations

Treat parsed mail as hostile input: headers, HTML, file names, and attachments need independent validation. Avoid loading large attachments into memory when a streaming approach is appropriate. No catalog AOT/trimming guarantee is asserted; test parse/build/send paths in the deployment mode.

## Avoid

Do not concatenate headers, trust attachment extensions/content types, or render untrusted HTML mail without a separate sanitization policy.

## Verification checklist

- Send representative plain-text, HTML, inline, and attachment messages to a test mailbox.
- Parse malformed and oversized samples according to the service limit policy.
- Assert header encoding and recipient validation with non-ASCII data.

## Sources

- https://www.nuget.org/packages/MimeKit/4.17.0 (Accessed 2026-07-27)
- https://github.com/jstedfast/MimeKit (Accessed 2026-07-27)
- https://mimekit.net/docs (Accessed 2026-07-27)
