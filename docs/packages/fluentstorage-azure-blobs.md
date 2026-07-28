# FluentStorage.Azure.Blobs

## Catalog entry

| Field | Value |
| --- | --- |
| Package | `FluentStorage.Azure.Blobs` |
| Pinned version | `8.0.10` |
| Role | Azure Blob Storage implementation of FluentStorage `IStore` |
| Status | Companion; approved for blob workloads; use the native Azure client when blob-specific conditions, encryption options, or leases are required |
| Owner | IX |
| Last reviewed | 2026-07-27 |
| Review trigger | Any provider/Azure Blob SDK or target-framework change, or a change to identity, retry, endpoint, conditional-write, or storage-account policy |

## Decision and scope

Use this provider for Azure Blob Storage, not Azure Files. Containers and blob names are object-store concepts: `/` creates a virtual prefix only. Blob metadata, blob versions, tags, access tiers, leases, immutability, customer-provided keys, and conditional ETag writes are not a portable `IStore` contract.

## Recommended registration and use

Reference the provider without a version; central package management remains authoritative:

```xml
<ItemGroup>
  <PackageReference Include="FluentStorage.Azure.Blobs" />
</ItemGroup>
```

Prefer an existing `BlobServiceClient` configured with `DefaultAzureCredential` and Azure SDK options, then wrap it. This preserves passwordless authentication and makes the Azure SDK the single owner of retry, transport, logging, and client encryption configuration.

```csharp
using Azure.Identity;
using Azure.Storage.Blobs;
using FluentStorage;
using FluentStorage.Azure.Blobs;

var client = new BlobServiceClient(
    new Uri("https://modularbasestorage.blob.core.windows.net"),
    new DefaultAzureCredential());

IAzureBlobStore store = AzureBlobStorage.FromClient(client, "documents");
```

Use a managed identity in hosted environments and Entra roles scoped to data-plane access. `FromTokenCredential` and `FromMsi` are available alternatives, but `FromClient` is the clearest way to retain an explicitly configured Azure SDK client. Shared key/SAS helpers are for tightly controlled compatibility cases; do not make account keys the default credential.

Provision the lowercase `documents` container separately, assign the workload a data-plane role such as Storage Blob Data Contributor at the narrowest practical scope, and enable the required firewall/private endpoint before startup. After construction, use the shared upload/download/list/delete workflow in [FluentStorage](fluentstorage.md). Keep `FolderPath` prefix-bounded; recursive deletion enumerates blobs and is not atomic. If a workflow requires create-if-absent, replace-if-current, a lease, tags, a tier, or a version ID, perform that operation with `BlobContainerClient`/`BlobClient` in a provider-specific adapter.

Dispose returned FluentStorage streams. For large objects, use streaming APIs. `OpenWrite` commits when the returned stream is disposed, and generic `IStore` calls cannot express every Azure conditional or lease requirement.

| Setting | Required/typical value | Operational note |
| --- | --- | --- |
| Service URI | Exact `https://{account}.blob.core.windows.net` endpoint or approved sovereign/private equivalent | Validate scheme, account and DNS; never log a SAS query string. |
| Container | Pre-provisioned lowercase container | Fail startup when missing; authorize it independently from account management access. |
| Credential | `DefaultAzureCredential`/workload identity | Confirm the resolved principal and data-plane RBAC scope; avoid shared keys. |
| `BlobClientOptions.Retry` | Bounded attempts, delay, network timeout, and operation timeout | Keep Azure SDK as retry owner; budget unknown write outcomes. |
| Prefix/object limit | Authorized prefix and workload size/list limits | Bound listing and materialized reads; use native conditions when atomicity matters. |

## Enterprise implementation guidance

- Use Microsoft Entra workload identity/managed identity and the least-privilege `Storage Blob Data ...` role. Store no account keys in application configuration.
- Azure SDK Blob clients have built-in retries. Tune `BlobClientOptions.Retry` on the client passed to `FromClient`; do not add an unbounded Polly/Microsoft resilience retry layer. Retry only classified transient, idempotent operations and account for unknown write outcomes.
- Enforce encryption at rest and network controls at the storage account. Use native SDK APIs for customer-managed keys, customer-provided keys, or client-side encryption; FluentStorage’s shared abstraction does not configure them.
- Capture Azure request IDs, service status, sanitized container/prefix, bytes and duration. Exclude SAS query strings, account keys, bearer tokens, and sensitive blob names from telemetry.

### Upgrade and rollback

Upgrade `FluentStorage.Azure.Blobs`, core FluentStorage, and the resolved Azure Storage/Identity clients as one tested set. Compile the actual `FromClient` path.
Test credential resolution, RBAC/firewall access, and authorization failures.
Also integration-test overwrite, prefix listing, large streams, cancellation, SDK retries, and any native ETag/lease/version operations used beside the abstraction. Recheck default retry and identity-chain behavior after transitive SDK changes.

Rollback to the prior package graph and `BlobServiceClient` options without changing the account, container, prefix, or identity scope. Drain transfers and reconcile writes that timed out using blob ETag/version/checksum before replay. Blobs, versions, leases, and uncommitted blocks created by the failed release remain external state; handle them through native diagnostics/lifecycle policy.

## Integration with the catalog

- Shared path, stream, and retry guidance: [FluentStorage](fluentstorage.md).
- File-share workload instead: [FluentStorage.Azure.Files](fluentstorage-azure-files.md).
- Coordinate outer resiliency with the catalog Polly/Microsoft resilience entries only after Azure SDK retry ownership is defined.
- Selection boundary: [Storage abstraction and provider SDKs](../package-guidance/package-selection.md#storage-abstraction-and-provider-sdks).
- End-to-end workflow: [Portable storage upload and download](../recipes/fluentstorage-portable-transfer.md).
- Provenance and dependency review: [FluentStorage.Azure.Blobs supply-chain entry](../package-guidance/supply-chain.md#fluentstorage-azure-blobs).

## Security, performance, AOT, trimming, and operations

Containers are lowercase; blob names are not filesystem paths. List by prefix rather than relying on directory behavior. Use native APIs when atomicity depends on ETags, leases, or version IDs—`ObjectExists` then write is racy. Configure lifecycle, soft delete, versioning, immutability, replication, private endpoints, firewall rules, and diagnostic settings outside this package.

Azure Blob streams can be network-backed. Keep them short-lived, propagate cancellation, and avoid `GetBytes`/`GetText` for unbounded content. The package is not documented as trimming- or Native-AOT-safe; test the exact wrapped Azure client and provider path before making that claim.

### Operational signals

Measure requests, latency, bytes, SDK retries, throttling, `4xx`/`5xx`, active transfers, bounded-list result counts, and unknown outcomes by operation/container. Preserve `x-ms-request-id` and client request ID with a sanitized prefix. Alert on sustained `403`, `409`/`412`, throttling/service-unavailable responses, latency, or reconciliation backlog; exclude SAS values, keys, tokens, payloads, and sensitive full blob names.

### Troubleshooting

| Symptom | Likely cause | Diagnostic | Correction | Retry? |
| --- | --- | --- | --- | --- |
| `403` authorization failure | Wrong principal/RBAC scope, firewall/private endpoint, disabled shared key, expired SAS, or encryption policy | Capture Azure error code and request IDs; verify resolved identity, data-plane role, endpoint/DNS, firewall and account policy | Correct identity/RBAC/network policy; prefer Entra identity | No; retry after correction/propagation only |
| `404 BlobNotFound`/container missing | Wrong account/container/prefix or provisioning race | Compare sanitized URI/container/key with resource inventory and request ID | Provision/choose the authoritative container and correct key construction | No, unless a documented provisioning operation is still converging |
| `409`/`412` on write or delete | Lease, immutability, snapshot, ETag, or overwrite condition conflict | Inspect the service error code, ETag/version/lease state and account policy | Resolve through native conditional/lease/version APIs; do not bypass policy | No automatic retry; refresh state and make a new decision |
| `429`/`5xx` or transient timeout | Service throttling/outage or network path failure | Inspect Azure metrics, SDK attempt count, request IDs and private endpoint health | Let the bounded SDK policy back off; reduce concurrency if sustained | Yes for idempotent operations; reconcile writes first |

## Avoid

- Do not use Azure Files for blob workloads, or treat blob prefixes as directories.
- Do not use shared account keys/SAS URLs in source, logs, or ordinary configuration.
- Do not rely on a generic overwrite for ETag/lease-protected writes.
- Do not claim client-side encryption merely because storage-account encryption at rest is enabled.

## Verification checklist

- [ ] Restore resolves `FluentStorage.Azure.Blobs` `8.0.10` with the catalog core package.
- [ ] A managed identity/Entra principal has only required container/prefix data-plane permissions.
- [ ] Integration tests cover read/write overwrite, streaming disposal, cancellation, and a conditional native-client write where the workload needs concurrency protection.
- [ ] Storage-account encryption, private networking/firewall, lifecycle/versioning, diagnostics, and retry options match the workload policy.
- [ ] No SAS/account key/token is present in configuration, logs, traces, or test fixtures.

## Sources

Accessed 2026-07-27.

- [FluentStorage Azure Blob factory/source](https://github.com/robinrodricks/FluentStorage/tree/develop/FluentStorage.Azure.Blobs)
- [FluentStorage.Azure.Blobs 8.0.10 on NuGet](https://www.nuget.org/packages/FluentStorage.Azure.Blobs/8.0.10)
- [Azure Blob .NET authentication guidance](https://learn.microsoft.com/azure/storage/blobs/storage-quickstart-blobs-dotnet)
- [Azure Blob .NET retry configuration](https://learn.microsoft.com/azure/storage/blobs/storage-retry-policy)
- [Azure Blob authorization with Microsoft Entra ID](https://learn.microsoft.com/azure/storage/blobs/authorize-access-azure-active-directory)
- [Azure Storage encryption at rest](https://learn.microsoft.com/azure/storage/common/storage-service-encryption)
- [Troubleshoot Azure Blob `403` errors](https://learn.microsoft.com/troubleshoot/azure/azure-storage/blobs/authentication/storage-troubleshoot-403-errors)
- [Azure Blob service error codes](https://learn.microsoft.com/rest/api/storageservices/blob-service-error-codes)
