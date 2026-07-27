# FluentStorage.Azure.Blobs

## Catalog entry

| Field | Value |
| --- | --- |
| Package | `FluentStorage.Azure.Blobs` |
| Pinned version | `8.0.10` |
| Role | Azure Blob Storage implementation of FluentStorage `IStore` |
| Status | Approved for blob workloads; use the native Azure client when blob-specific conditions, encryption options, or leases are required |

## Decision and scope

Use this provider for Azure Blob Storage, not Azure Files. Containers and blob names are object-store concepts: `/` creates a virtual prefix only. Blob metadata, blob versions, tags, access tiers, leases, immutability, customer-provided keys, and conditional ETag writes are not a portable `IStore` contract.

## Recommended registration and use

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

Dispose returned FluentStorage streams. For large objects, use streaming APIs. `OpenWrite` commits when the returned stream is disposed, and generic `IStore` calls cannot express every Azure conditional or lease requirement.

## Enterprise implementation guidance

- Use Microsoft Entra workload identity/managed identity and the least-privilege `Storage Blob Data ...` role. Store no account keys in application configuration.
- Azure SDK Blob clients have built-in retries. Tune `BlobClientOptions.Retry` on the client passed to `FromClient`; do not add an unbounded Polly/Microsoft resilience retry layer. Retry only classified transient, idempotent operations and account for unknown write outcomes.
- Enforce encryption at rest and network controls at the storage account. Use native SDK APIs for customer-managed keys, customer-provided keys, or client-side encryption; FluentStorage’s shared abstraction does not configure them.
- Capture Azure request IDs, service status, sanitized container/prefix, bytes and duration. Exclude SAS query strings, account keys, bearer tokens, and sensitive blob names from telemetry.

## Integration with the catalog

- Shared path, stream, and retry guidance: [FluentStorage](fluentstorage.md).
- File-share workload instead: [FluentStorage.Azure.Files](fluentstorage-azure-files.md).
- Coordinate outer resiliency with the catalog Polly/Microsoft resilience entries only after Azure SDK retry ownership is defined.

## Security, performance, AOT, trimming, and operations

Containers are lowercase; blob names are not filesystem paths. List by prefix rather than relying on directory behavior. Use native APIs when atomicity depends on ETags, leases, or version IDs—`ObjectExists` then write is racy. Configure lifecycle, soft delete, versioning, immutability, replication, private endpoints, firewall rules, and diagnostic settings outside this package.

Azure Blob streams can be network-backed. Keep them short-lived, propagate cancellation, and avoid `GetBytes`/`GetText` for unbounded content. The package is not documented as trimming- or Native-AOT-safe; test the exact wrapped Azure client and provider path before making that claim.

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
- [Azure Storage encryption at rest](https://learn.microsoft.com/azure/storage/common/storage-service-encryption)
