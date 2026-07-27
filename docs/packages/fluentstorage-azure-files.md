# FluentStorage.Azure.Files

## Catalog entry

| Field | Value |
| --- | --- |
| Package | `FluentStorage.Azure.Files` |
| Pinned version | `8.0.10` |
| Role | Azure Files implementation of FluentStorage `IStore` |
| Status | Approved for file-share workloads; not interchangeable with Azure Blob Storage |
| Owner | IX |
| Last reviewed | 2026-07-27 |
| Review trigger | Any provider/Azure Files SDK or target-framework change, or a change to REST identity support, share limits, retry defaults, endpoint, SMB/NFS, or storage-account policy |

## Decision and scope

Use Azure Files when the workload requires an Azure file share and directory/file semantics. It is not an object-store provider: share paths, directory creation, SMB/NFS integration, quotas, backups, and share snapshots are Azure Files concerns. Do not select it merely because its `IStore` calls resemble Azure Blob Storage.

## Recommended registration and use

Reference the provider through the centrally managed catalog:

```xml
<ItemGroup>
  <PackageReference Include="FluentStorage.Azure.Files" />
</ItemGroup>
```

Prefer an existing `ShareServiceClient` built with `DefaultAzureCredential`, then wrap it:

```csharp
using Azure.Identity;
using Azure.Storage.Files.Shares;
using FluentStorage;
using FluentStorage.Storage;

var client = new ShareServiceClient(
    new Uri("https://modularbasestorage.file.core.windows.net"),
    new DefaultAzureCredential());

IStore store = AzureFilesStorage.FromClient(client);
```

Use Entra workload identity or managed identity in production. The provider also supports token, managed identity, shared key, and existing-client factories. Prefer `FromClient` when SDK retry, transport, observability, and identity configuration must be explicit. A share is selected through the storage path/workload configuration; validate and scope that selection rather than taking it from an untrusted request.

Provision the share and its quota separately and verify that the selected REST authentication model supports the intended operations; SMB/NFS mount identity is a different boundary from REST client authorization. Use the shared upload/download/list/delete pattern from [FluentStorage](fluentstorage.md), but treat the first path segment as an authorized share name and later segments as directories/files. Prefer `DeleteObject` for a file. Use `DeleteDirectory` only when directory semantics are intended, and require an explicit `recursive` choice because a non-empty directory cannot be treated like an object-store prefix.

Dispose streams returned by the store. Azure Files may need a seekable stream for some operations; test the actual upload stream shape and size. Use `GetBytes` only for explicitly bounded files.

| Setting | Required/typical value | Operational note |
| --- | --- | --- |
| Service URI | Exact `https://{account}.file.core.windows.net` endpoint or approved equivalent | REST endpoint identity differs from SMB/NFS mount identity; validate DNS/TLS/private endpoint routing. |
| Share/path root | Pre-provisioned authorized share and directory root | Treat the first segment as a share; reject client-selected shares and traversal-like segments. |
| Credential | Entra workload identity where supported by the chosen REST operations | Validate data-plane RBAC; isolate and rotate a shared key only when compatibility requires it. |
| `ShareClientOptions.Retry` | Bounded attempts and network/operation timeout | Keep the Azure SDK as retry owner and document write reconciliation. |
| Quota/file limits | Provisioned share quota and workload transfer/list limits | Monitor share usage and validate maximum file size/seekability for the actual client path. |

## Enterprise implementation guidance

- Use Entra identities and Azure Files data-plane RBAC where supported for the chosen protocol/client. If shared keys are required by a compatibility boundary, retrieve and rotate them through a secret manager and never log them.
- Tune Azure SDK retry/timeouts on the `ShareServiceClient` supplied to `FromClient`. Avoid stacking generic retries on non-idempotent or unknown-outcome writes.
- Configure encryption at rest, private endpoints/firewall, secure transfer, share quotas, backup/snapshot policy, and protocol-specific authorization in Azure. The shared abstraction does not provision these controls.
- Trace file operation, sanitized share/directory prefix, bytes, duration, status and request ID. Never trace account keys, SAS values, tokens, or sensitive path segments.

### Upgrade and rollback

Upgrade the provider, FluentStorage core, and resolved Azure Files/Identity SDKs together. Compile the actual `FromClient` construction and run integration tests for REST authentication, share selection, nested-directory creation, overwrite/delete behavior, large and non-seekable streams, cancellation, retry attempts, quota behavior, and any native ETag/lease/snapshot operations. Reconfirm that the selected operations support the intended Entra authorization mode.

Rollback to the prior verified package graph and `ShareServiceClient` options while preserving account, share, root, identity, and protocol configuration. Drain transfers first and reconcile uncertain writes by native file properties/ETag/length/checksum before replay. Files, directories, handles, snapshots, and quota consumed during the failed release are external state and are not reverted by redeployment.

## Integration with the catalog

- Shared streams, key validation, resilience, and disposal: [FluentStorage](fluentstorage.md).
- Object-store workload instead: [FluentStorage.Azure.Blobs](fluentstorage-azure-blobs.md).
- Use the catalog resilience entries only for a deliberately bounded outer policy.
- Selection boundary: [Storage abstraction and provider SDKs](../package-guidance/package-selection.md#storage-abstraction-and-provider-sdks).
- End-to-end workflow: [Portable storage upload and download](../recipes/fluentstorage-portable-transfer.md).
- Provenance and dependency review: [FluentStorage.Azure.Files supply-chain entry](../package-guidance/supply-chain.md#fluentstorage-azure-files).

## Security, performance, AOT, trimming, and operations

Azure Files has real directory semantics unlike Blob Storage and S3 prefixes, but `IStore` is still not a transactional filesystem API. `ObjectExists` followed by a write is a race; use native Azure Files concurrency/versioning controls when required. File/share limits, snapshots, SMB/NFS behavior, and identity support depend on the storage account, region, and protocol configuration.

Keep file streams short-lived and propagate cancellation. This package has no documented Native AOT/trimming guarantee. Verify the exact Azure SDK/provider/client path under publish-and-run conditions before relying on either deployment mode.

### Operational signals

Measure REST calls, latency, bytes, SDK attempts, throttles/errors, active transfers, directory-list counts, open-handle conflicts, share capacity/quota and unknown outcomes by share/operation. Correlate Azure request/client IDs with a sanitized share/root. Alert on sustained `403`, `404`, `409`, throttling/`5xx`, quota pressure, latency, or reconciliation backlog; exclude keys, SAS/tokens, payloads, and sensitive paths.

### Troubleshooting

| Symptom | Likely cause | Diagnostic | Correction | Retry? |
| --- | --- | --- | --- | --- |
| `403` from REST client | Unsupported auth mode for operation, wrong data-plane RBAC, firewall/private endpoint, or invalid shared credential | Capture Azure error/request IDs; verify the REST principal, role scope, endpoint and network policy (not SMB mount credentials) | Use a supported Entra flow/role or secret-managed compatibility credential; fix network scope | No; retry only after correction propagates |
| Share/path `404` | Wrong account/share/root or directory was not provisioned/created | Compare sanitized URI/share/path with inventory and parent-directory state | Correct configuration and explicitly provision required share/directories | No, except a known provisioning convergence |
| `409` conflict on delete/write/rename | Non-empty directory, open handle, lease/snapshot, or ETag condition | Inspect service error code, request ID, directory contents and native handle/property state | Close/revoke handles as policy allows; use native concurrency APIs; delete recursively only when authorized | No automatic retry; refresh state first |
| Capacity/timeout during upload | Share quota, file limit, network pressure, or incompatible stream behavior | Check share capacity metrics, file size, stream seekability, SDK attempts and request ID | Increase/provision quota deliberately, stream within limits, and tune bounded client options | Retry transient service/network failures only after reconciling the remote file |

## Avoid

- Do not use Azure Files as a drop-in blob-prefix model or assume blob access-tier/versioning semantics.
- Do not put account keys in connection strings or use a generic `IStore` overwrite as a concurrency primitive.
- Do not add an outer retry loop without accounting for Azure SDK retries and duplicate/unknown write results.
- Do not expose client-supplied share or path values without authorization and segment validation.

## Verification checklist

- [ ] Restore resolves `FluentStorage.Azure.Files` `8.0.10` without provider/core downgrade warnings.
- [ ] A workload identity can perform only required share/directory operations.
- [ ] Tests cover nested path creation, overwrite/concurrency behavior, returned-stream disposal, cancellation, and target-size streaming.
- [ ] Azure Files protocol, private networking, encryption, share quota, snapshot/backup, and diagnostics settings have been reviewed.
- [ ] Retry ownership and idempotency/reconciliation behavior are documented for each write operation.

## Sources

Accessed 2026-07-27.

- [FluentStorage Azure Files factory/source](https://github.com/robinrodricks/FluentStorage/tree/develop/FluentStorage.Azure.Files)
- [FluentStorage.Azure.Files 8.0.10 on NuGet](https://www.nuget.org/packages/FluentStorage.Azure.Files/8.0.10)
- [Azure Files .NET client library](https://learn.microsoft.com/azure/storage/files/storage-dotnet-how-to-use-files)
- [Authorize Azure Files access with Microsoft Entra ID over REST](https://learn.microsoft.com/azure/storage/files/authorize-oauth-rest)
- [Azure Files identity-based authorization](https://learn.microsoft.com/azure/storage/files/storage-files-identity-auth-domain-services-enable)
- [Azure Storage encryption at rest](https://learn.microsoft.com/azure/storage/common/storage-service-encryption)
