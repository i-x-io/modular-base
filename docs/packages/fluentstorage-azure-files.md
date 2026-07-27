# FluentStorage.Azure.Files

## Catalog entry

| Field | Value |
| --- | --- |
| Package | `FluentStorage.Azure.Files` |
| Pinned version | `8.0.10` |
| Role | Azure Files implementation of FluentStorage `IStore` |
| Status | Approved for file-share workloads; not interchangeable with Azure Blob Storage |

## Decision and scope

Use Azure Files when the workload requires an Azure file share and directory/file semantics. It is not an object-store provider: share paths, directory creation, SMB/NFS integration, quotas, backups, and share snapshots are Azure Files concerns. Do not select it merely because its `IStore` calls resemble Azure Blob Storage.

## Recommended registration and use

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

Dispose streams returned by the store. Azure Files may need a seekable stream for some operations; test the actual upload stream shape and size. Use `GetBytes` only for explicitly bounded files.

## Enterprise implementation guidance

- Use Entra identities and Azure Files data-plane RBAC where supported for the chosen protocol/client. If shared keys are required by a compatibility boundary, retrieve and rotate them through a secret manager and never log them.
- Tune Azure SDK retry/timeouts on the `ShareServiceClient` supplied to `FromClient`. Avoid stacking generic retries on non-idempotent or unknown-outcome writes.
- Configure encryption at rest, private endpoints/firewall, secure transfer, share quotas, backup/snapshot policy, and protocol-specific authorization in Azure. The shared abstraction does not provision these controls.
- Trace file operation, sanitized share/directory prefix, bytes, duration, status and request ID. Never trace account keys, SAS values, tokens, or sensitive path segments.

## Integration with the catalog

- Shared streams, key validation, resilience, and disposal: [FluentStorage](fluentstorage.md).
- Object-store workload instead: [FluentStorage.Azure.Blobs](fluentstorage-azure-blobs.md).
- Use the catalog resilience entries only for a deliberately bounded outer policy.

## Security, performance, AOT, trimming, and operations

Azure Files has real directory semantics unlike Blob Storage and S3 prefixes, but `IStore` is still not a transactional filesystem API. `ObjectExists` followed by a write is a race; use native Azure Files concurrency/versioning controls when required. File/share limits, snapshots, SMB/NFS behavior, and identity support depend on the storage account, region, and protocol configuration.

Keep file streams short-lived and propagate cancellation. This package has no documented Native AOT/trimming guarantee. Verify the exact Azure SDK/provider/client path under publish-and-run conditions before relying on either deployment mode.

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
- [Azure Files identity-based authorization](https://learn.microsoft.com/azure/storage/files/storage-files-identity-auth-domain-services-enable)
- [Azure Storage encryption at rest](https://learn.microsoft.com/azure/storage/common/storage-service-encryption)
