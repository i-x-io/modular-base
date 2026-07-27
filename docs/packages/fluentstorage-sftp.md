# FluentStorage.SFTP

## Catalog entry

| Field | Value |
| --- | --- |
| Package | `FluentStorage.SFTP` |
| Pinned version | `8.0.16` |
| Role | SSH.NET-backed SFTP implementation of FluentStorage `IStore` |
| Status | Approved for managed SFTP interchange; isolate it from object-store abstractions when protocol semantics matter |

## Decision and scope

Use this provider for an SFTP server where file/directory semantics and SSH transport are the integration contract. It is not an object store: remote paths, permissions, atomic rename behavior, quota, server-side retention, and host-key trust are server-specific. The implementation has an internal retry policy, so account for it before introducing another resilience layer.

## Recommended registration and use

Reference the provider without a version; the central catalog supplies `8.0.16` and its SSH.NET dependency:

```xml
<ItemGroup>
  <PackageReference Include="FluentStorage.SFTP" />
</ItemGroup>
```

Prefer private-key authentication. This factory example configures authentication only; host-key verification is a separate requirement discussed below:

```csharp
using FluentStorage;
using FluentStorage.Storage;
using Renci.SshNet;

var key = new PrivateKeyFile("/run/secrets/sftp-client-key");
IStore store = SftpStorage.FromPrivateKey(
    host: "sftp.partner.example",
    port: 22,
    username: "modular-base",
    keyFiles: key);
```

The simple password factory is a compatibility option only; retrieve any password through a secret manager. Dispose the store at host shutdown and each returned input/read stream after the operation.

Production setup must validate host identity. Obtain the expected SHA-256 host-key fingerprint over a separately trusted channel and treat a mismatch as a hard failure. SSH.NET exposes `HostKeyReceived`, but the pinned FluentStorage provider's `FromClient` factory also constructs a second `SshClient` from the same `ConnectionInfo` for command operations without exposing that client for a host-key handler. Do not claim complete host-key pinning through the public provider factory without an integration test of every used operation; use a reviewed native SSH.NET adapter or an upstream fix when this is a hard security requirement. Provision a restricted server account/root directory and test permissions for list, create, rename, read, and delete. Use the shared `SetObject`, `OpenRead`, `ListObjects`, and `DeleteObject` workflow from [FluentStorage](fluentstorage.md); for partner exchange, upload to a unique staging name and call `MoveObject(staging, final, overwrite: false, cancellationToken)` only after the upload stream is closed and any required checksum is verified.

## Enterprise implementation guidance

- Use private keys, least-privilege server accounts, restricted roots/chroots, rotation, and an approved secret manager. Do not use password strings in source, connection strings, logs, or exception messages.
- Pin and monitor server host keys. A secure SSH transport depends on host authenticity, not merely port 22 and an encrypted session.
- This provider contains a retry policy that retries `Exception` three times in its source. Do not add a broad Polly/Microsoft resilience policy around every operation; doing so multiplies attempts and may repeat non-idempotent append or transfer work. Use a narrowly classified outer policy only with an explicit overall timeout and reconciliation plan.
- Trace sanitized server identity, remote root/prefix, operation, bytes, duration, SSH failure class and correlation ID. Do not trace credentials, private-key material, full sensitive paths, or payloads.

## Integration with the catalog

- Shared stream ownership, path construction, and general retry guidance: [FluentStorage](fluentstorage.md).
- Object-store providers have materially different semantics: [AWS](fluentstorage-aws.md), [Azure Blobs](fluentstorage-azure-blobs.md), [GCP](fluentstorage-gcp.md), and [MinIO](fluentstorage-minio.md).
- Coordinate outer resilience only with a documented budget alongside this provider’s built-in retry behavior.

## Security, performance, AOT, trimming, and operations

SFTP has real remote directories. The provider ensures directories for relevant writes, but directory operations and permissions are server behavior. It is inappropriate to treat SFTP as an S3 bucket: no object versions, tags, storage tiers, lifecycle, or cloud-native encryption policy follows from the shared interface.

For reliable partner delivery, write to a unique staging name, close/dispose the stream to complete the upload, then use an agreed server-side atomic rename/publish step where supported. Do not use `ObjectExists` then write as a concurrency control. Validate seek/range behavior, large file limits, timeout handling, and retry effects against the specific server. No trimming/Native-AOT guarantee is documented.

SFTP servers vary in whether rename-without-overwrite is atomic and how they expose fsync/durability. Confirm the actual server behavior and partner pickup convention; do not promise atomic publication solely because `MoveObject` returned successfully. Keep recursive listings bounded because this provider walks directory trees client-side.

## Avoid

- Do not accept unknown host keys automatically in production.
- Do not use passwords as the default when private-key or certificate-backed credentials are available.
- Do not blindly retry append, rename, or a timed-out upload without checking whether the remote file was already published.
- Do not assume filesystem-style paths are safe: authorize the logical destination and reject traversal-like or untrusted segments before building paths.

## Verification checklist

- [ ] Restore resolves `FluentStorage.SFTP` `8.0.16` with the selected core package.
- [ ] Integration configuration uses private-key/secret-managed authentication and verified host identity.
- [ ] Tests cover directory creation, large streaming upload/download, stream disposal, cancellation/timeout, retry attempt budget, and duplicate/unknown-outcome recovery.
- [ ] Partner publication protocol (staging, checksum, rename/acknowledgement, retention) is documented and tested on the actual server.
- [ ] Server account/root permissions and telemetry redaction have been reviewed.

## Sources

Accessed 2026-07-27.

- [FluentStorage SFTP factory/source](https://github.com/robinrodricks/FluentStorage/tree/develop/FluentStorage.SFTP)
- [FluentStorage.SFTP 8.0.16 on NuGet](https://www.nuget.org/packages/FluentStorage.SFTP/8.0.16)
- [SSH.NET project documentation](https://github.com/sshnet/SSH.NET)
- [SSH.NET getting started and supported host-key algorithms](https://sshnet.github.io/SSH.NET/)
- [SSH.NET `SftpClient` API](https://sshnet.github.io/SSH.NET/api/Renci.SshNet.SftpClient.html)
