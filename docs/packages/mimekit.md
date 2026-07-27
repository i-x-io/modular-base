# MimeKit

## Catalog entry

`MimeKit` **4.17.0** — direct catalog package; MIME message, body, address, header, and attachment construction/parsing library.

## Decision and scope

Use to model messages and attachments correctly before handing them to MailKit. It does not send or receive mail transport traffic.

## Recommended registration and use

The catalog supplies the version centrally, so the composition project keeps the reference versionless:

```xml
<ItemGroup>
  <PackageReference Include="MimeKit" />
</ItemGroup>
```

MimeKit needs no dependency-injection registration. Build structured addresses and use `BodyBuilder` for a text/HTML alternative plus attachments:

```csharp
using MimeKit;
using MimeKit.Utils;

static MimeMessage ComposeReceipt(byte[] receiptPdf)
{
    var message = new MimeMessage
    {
        Subject = "Your receipt",
        MessageId = MimeUtils.GenerateMessageId("example.com")
    };
    message.From.Add(new MailboxAddress("Example Billing", "billing@example.com"));
    message.To.Add(new MailboxAddress("Ada", "ada@example.net"));

    var body = new BodyBuilder
    {
        TextBody = "Your receipt is attached.",
        HtmlBody = "<p>Your receipt is attached.</p>"
    };
    body.Attachments.Add(
        "receipt.pdf",
        receiptPdf,
        ContentType.Parse("application/pdf"));
    message.Body = body.ToMessageBody();
    return message;
}
```

Hand the completed `MimeMessage` to MailKit for transport. Preserve the original parsed message when byte-level fidelity is required rather than reconstructing headers from strings.

## Enterprise implementation guidance

The usual outbound workflow is: validate recipients and template data, render both plain-text and HTML variants, construct the MIME tree, attach bounded content with an explicit media type, assign a stable message identifier, then hand the immutable work item to the transport worker. Keep template rendering separate from MIME construction and audit metadata outside the user-visible body. For inbound mail, parse under size/depth limits, enumerate attachments, sanitize file names with `Path.GetFileName`, scan decoded bytes, and quarantine before storage or display.

## Integration with the catalog

`mailkit.md` delivers the resulting message. `anglesharp.md` may process HTML templates or inbound HTML, but parsing does not sanitize it for display.

## Security, performance, AOT, trimming, and operations

Treat parsed mail as hostile input: headers, HTML, Unicode display names, file names, nested messages, and attachments need independent validation. MIME parsing and correct encoding do not sanitize HTML. Do not infer trust from an extension or declared content type, prevent path traversal when extracting files, and cap encoded and decoded sizes to resist archive/decompression abuse. Stream large content where practical and dispose owned streams according to the overload contract. No catalog AOT/trimming guarantee is asserted; test parse/build/serialize paths in the deployment mode.

## Avoid

Do not concatenate headers, trust attachment extensions/content types, or render untrusted HTML mail without a separate sanitization policy.

## Verification checklist

- [ ] Serialize representative plain-text, HTML, inline, and attachment messages and inspect their MIME structure.
- [ ] Parse malformed, nested, and oversized samples according to the service limit policy.
- [ ] Assert header/address encoding with non-ASCII and internationalized data.
- [ ] Verify extracted file names cannot escape the quarantine directory and decoded content is scanned.

## Sources

- [NuGet Gallery: MimeKit 4.17.0](https://www.nuget.org/packages/MimeKit/4.17.0) (Accessed 2026-07-27)
- [MimeKit 4.17.0 API: `MimeMessage.Body`](https://mimekit.net/docs/html/P_MimeKit_MimeMessage_Body.htm) (Accessed 2026-07-27)
- [MimeKit 4.17.0 API: `BodyBuilder.Attachments`](https://mimekit.net/docs/html/P_MimeKit_BodyBuilder_Attachments.htm) (Accessed 2026-07-27)
- [MimeKit upstream FAQ and message-building examples](https://github.com/jstedfast/MimeKit/blob/4.17.0/FAQ.md) (Accessed 2026-07-27)
