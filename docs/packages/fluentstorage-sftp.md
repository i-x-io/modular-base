# FluentStorage.SFTP

## Catalog entry

| Field | Value |
| --- | --- |
| Package | `FluentStorage.SFTP` |
| Pinned version | `8.0.16` |
| Role | SSH.NET-backed SFTP implementation of FluentStorage `IStore` |
| Status | Approved for managed SFTP interchange; isolate it from object-store abstractions when protocol semantics matter |
| Owner | IX |
| Last reviewed | 2026-07-27 |
| Review trigger | Any `FluentStorage.SFTP`, SSH.NET, target-framework, server SSH policy/host key, cipher/KEX, retry, path, quota, or partner-protocol change |

## Decision and scope

Use this provider for an SFTP server where file/directory semantics and SSH transport are the integration contract. It is not an object store: remote paths, permissions, atomic rename behavior, quota, server-side retention, and host-key trust are server-specific. The implementation has an internal retry policy, so account for it before introducing another resilience layer.

## Recommended registration and use

Reference the provider without a version; the central catalog supplies `8.0.16` and its SSH.NET dependency:

```xml
<ItemGroup>
  <PackageReference Include="FluentStorage.SFTP" />
</ItemGroup>
```

Prefer private-key authentication. The public FluentStorage factory does not expose host-key validation for every client it creates, so the following authentication-only example is **local-development/test only**. It must not be copied into a production integration:

```csharp
using FluentStorage;
using FluentStorage.Storage;
using Renci.SshNet;

// LOCAL/TEST ONLY: this factory does not establish production host-key trust.
var key = new PrivateKeyFile("./test-fixtures/sftp-client-key");
IStore store = SftpStorage.FromPrivateKey(
    host: "localhost",
    port: 22,
    username: "modular-base",
    keyFiles: key);
```

The simple password factory is a compatibility option only; retrieve any password through a secret manager. Dispose the store at host shutdown and each returned input/read stream after the operation.

Production setup must validate host identity before any file operation. Obtain the expected SHA-256 host-key fingerprint over a separately trusted channel and treat a mismatch as a hard failure. SSH.NET exposes `HostKeyReceived`; when host-key pinning is required, use a reviewed native SSH.NET adapter that registers that handler on **every** SSH client it creates before connecting, or wait for an upstream FluentStorage fix that provides the same guarantee. Do not deploy the public FluentStorage factory as a production SFTP client until an integration test proves validation coverage for every operation the integration uses. Provision a restricted server account/root directory and test permissions for list, create, rename, read, and delete. Use the shared `SetObject`, `OpenRead`, `ListObjects`, and `DeleteObject` workflow from [FluentStorage](fluentstorage.md); for partner exchange, upload to a unique staging name and call `MoveObject(staging, final, overwrite: false, cancellationToken)` only after the upload stream is closed and any required checksum is verified.

| Setting | Required/typical value | Operational note |
| --- | --- | --- |
| Host/port | Approved DNS name and explicit port (normally 22) | Validate routing and never bypass host identity because DNS resolves. |
| Host-key fingerprint | Pinned SHA-256 fingerprint obtained out of band | Treat changes as a controlled partner/security event, not an automatic retry. |
| Authentication | Private key plus secret-managed passphrase where needed | Restrict file permissions, rotation and server account scope; avoid password defaults. |
| Remote root/staging | Authorized chroot/root and unique staging convention | Validate segments and publish only after close/checksum; do not expose arbitrary paths. |
| Timeout/retry budget | SSH connect/operation timeout plus provider’s three attempts | Do not multiply retries; define reconciliation for upload/rename uncertainty. |

## Enterprise implementation guidance

- Use private keys, least-privilege server accounts, restricted roots/chroots, rotation, and an approved secret manager. Do not use password strings in source, connection strings, logs, or exception messages.
- Pin and monitor server host keys. A secure SSH transport depends on host authenticity, not merely port 22 and an encrypted session.
- This provider contains a retry policy that retries `Exception` three times in its source. Do not add a broad Polly/Microsoft resilience policy around every operation; doing so multiplies attempts and may repeat non-idempotent append or transfer work. Use a narrowly classified outer policy only with an explicit overall timeout and reconciliation plan.
- Trace sanitized server identity, remote root/prefix, operation, bytes, duration, SSH failure class and correlation ID. Do not trace credentials, private-key material, full sensitive paths, or payloads.

### Upgrade and rollback

Upgrade `FluentStorage.SFTP`, core FluentStorage, and SSH.NET together. Compile the private-key/client construction and integration-test every used operation against the partner/server: allowed host-key algorithms, pinning coverage, authentication, chroot/path permissions, nested directories, large transfers, cancellation/timeouts, provider attempt count, staging rename, duplicate recovery, and server limits. Coordinate host-key or cipher/KEX policy changes separately from a library rollout.

Rollback the prior package graph and SSH configuration without accepting an old/unknown host key or silently switching authentication. Drain transfers, retain staging files, and reconcile remote size/checksum/final-name state before replay. Files already published or partially uploaded are external state; follow the agreed partner retention/acknowledgement protocol rather than deleting them automatically.

## Integration with the catalog

- Shared stream ownership, path construction, and general retry guidance: [FluentStorage](fluentstorage.md).
- Object-store providers have materially different semantics: [AWS](fluentstorage-aws.md), [Azure Blobs](fluentstorage-azure-blobs.md), [GCP](fluentstorage-gcp.md), and [MinIO](fluentstorage-minio.md).
- Coordinate outer resilience only with a documented budget alongside this provider’s built-in retry behavior.
- Selection boundary: [Storage abstraction and provider SDKs](../package-guidance/package-selection.md#storage-abstraction-and-provider-sdks).
- End-to-end workflow: [Portable storage upload and download](../recipes/fluentstorage-portable-transfer.md).
- Provenance and dependency review: [FluentStorage.SFTP supply-chain entry](../package-guidance/supply-chain.md#fluentstorage-sftp).

## Security, performance, AOT, trimming, and operations

SFTP has real remote directories. The provider ensures directories for relevant writes, but directory operations and permissions are server behavior. It is inappropriate to treat SFTP as an S3 bucket: no object versions, tags, storage tiers, lifecycle, or cloud-native encryption policy follows from the shared interface.

For reliable partner delivery, write to a unique staging name, close/dispose the stream to complete the upload, then use an agreed server-side atomic rename/publish step where supported. Do not use `ObjectExists` then write as a concurrency control. Validate seek/range behavior, large file limits, timeout handling, and retry effects against the specific server. No trimming/Native-AOT guarantee is documented.

SFTP servers vary in whether rename-without-overwrite is atomic and how they expose fsync/durability. Confirm the actual server behavior and partner pickup convention; do not promise atomic publication solely because `MoveObject` returned successfully. Keep recursive listings bounded because this provider walks directory trees client-side.

### Operational signals

Measure connection/authentication failures, operations, duration, bytes, provider attempts, active transfers, directory-list counts, staging-file age/count, rename conflicts, remote quota/free-space failures and unknown outcomes. Log only a sanitized server identity/root and failure class/correlation ID. Alert on host-key mismatch immediately and on sustained authentication/connectivity failures, latency, attempt multiplication, stale staging files, quota pressure, or acknowledgement backlog; never emit credentials, key material, payloads, or sensitive full paths.

### Troubleshooting

| Symptom | Likely cause | Diagnostic | Correction | Retry? |
| --- | --- | --- | --- | --- |
| Host-key mismatch/algorithm negotiation failure | Server key rotated unexpectedly or SSH policy/cipher/KEX incompatibility | Compare observed fingerprint/algorithm with the independently approved value and coordinated server change | Verify out of band, update pin/policy through change control, or restore compatible server policy | No automatic retry or trust-on-first-use |
| Authentication failure | Wrong user/key/passphrase, expired/disabled account, or server auth policy change | Inspect sanitized SSH failure class and server auth logs; verify key fingerprint and account status | Rotate/restore the approved key/account/policy through secret management | No; retry after correction only |
| Permission/no-such-path/quota error | Wrong chroot/root, missing parent, filesystem permissions, quota or disk full | Check sanitized remote root/path, account permissions, server free space/quota and SFTP status | Correct authorized path/permissions or capacity; create parents deliberately | No until condition is corrected |
| Timeout during upload/rename | Network interruption or server completed work before response was lost | Inspect provider attempt count; compare staging/final remote size/checksum and partner acknowledgement | Reconcile and either resume/re-upload staging or publish exactly once per protocol | Only after state inspection proves the operation idempotent |

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
