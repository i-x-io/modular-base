# FluentStorage

## Catalog entry

| Field | Value |
| --- | --- |
| Package | `FluentStorage` |
| Pinned version | `8.0.16` |
| Role | Core `IStore` abstraction, local/in-memory stores, connection-string infrastructure, and shared models |
| Status | Direct; approved for application-facing storage abstractions; select and register exactly one concrete provider per storage boundary |
| Owner | IX |
| Last reviewed | 2026-07-27 |
| Review trigger | Any `FluentStorage` version change, target-framework change, or change to stream ownership, path normalization, listing, overwrite, or disposal behavior |

## Decision and scope

Use `IStore` when an application needs to isolate business code from a selected storage backend. This package does **not** make provider semantics identical: buckets and blobs use object keys and prefixes, whereas Azure Files and SFTP expose directories. Keep provider-specific requirements (retention, legal hold, immutable/versioned writes, access tiers, server-side encryption, and access policy) in the provider adapter or provisioning layer.

`FluentStorage` is the shared dependency for every approved provider in this catalog. It includes in-memory and disk implementations suitable for deterministic tests and local development; neither is a production substitute for an object-store durability or access-control design.

## Recommended registration and use

With central package management, the project file contains a versionless reference; the catalog keeps `8.0.16` authoritative:

```xml
<ItemGroup>
  <PackageReference Include="FluentStorage" />
</ItemGroup>
```

Register a long-lived `IStore` at the composition root and dispose it when the application host stops. Use a provider factory only at that boundary; application services should receive `IStore` or a narrow application-owned interface.

```csharp
using FluentStorage;
using FluentStorage.Storage;

IStore store = StorageFactory.Disk("/var/lib/modular-base/uploads");

await store.SetText("invoices/2026-07-27.json", "{\"status\":\"accepted\"}");
using Stream content = await store.OpenRead("invoices/2026-07-27.json");
```

A typical bounded workflow uses a caller-owned upload stream, a disposed download stream, an explicitly bounded listing, and an explicit delete:

```csharp
using FluentStorage.Model;
using FluentStorage.Storage;

const string key = "tenant/acme/document/42.pdf";

await store.SetObject(
    key,
    uploadStream,
    contentType: "application/pdf",
    cancellationToken: cancellationToken);

using (Stream download = await store.OpenRead(key, cancellationToken))
{
    await download.CopyToAsync(destinationStream, cancellationToken);
}

List<StoreObject> documents = await store.ListObjects(
    new StorageListOptions
    {
        FolderPath = "tenant/acme/document",
        Recurse = true,
        MaxResults = 100,
        IncludeAttributes = true
    },
    cancellationToken);

if (await store.ObjectExists(key, cancellationToken))
{
    await store.DeleteObject(key, cancellationToken);
}
```

`ObjectExists` before delete is useful for user-facing reporting, but it is not required for correctness and introduces another request. Deleting a missing object and listing behavior remain provider-specific. `ListObjects` returns a materialized `List<StoreObject>`; use `FolderPath`, `MaxResults`, and provider-appropriate recursion to prevent an accidental full-store scan. `DeleteObject` deletes one object, while `DeleteDirectory(path, recursive: true)` is a separate, potentially expensive operation and should require an authorized, narrowly scoped prefix.

Treat the path as a provider-neutral *object identifier*, not an OS file path. FluentStorage normalizes separators to `/` and strips leading/trailing separators, but it preserves `.` and `..` segments. Build keys from validated logical segments; never accept an unvalidated client path as a key. Use a stable prefix such as `tenant/{tenantId}/document/{documentId}` and retain the provider's canonical identifier in audit data.

Use `SetObject`, `OpenRead`, `OpenWrite`, `OpenRange`, or `OpenSeekable` for potentially large content. The caller owns every stream returned by `OpenRead`, `OpenRange`, `OpenSeekable`, and `OpenWrite`; disposing an `OpenWrite` stream commits its content. `GetBytes` and `GetText` materialize the full object and are only appropriate for bounded payloads. Do not dispose a caller-supplied input stream until `SetObject` has completed.

For local development, create the disk root before startup and grant the process identity access only to that directory. Use `StorageFactory.InMemory()` in unit tests, recreate it per test, and never infer production durability, concurrency, or listing semantics from the in-memory provider. At shutdown, stop accepting work, await in-flight transfers, dispose their streams, and finally dispose the singleton store.

Configuration belongs at the composition root and must be validated before the store is constructed:

| Setting | Required/typical value | Operational note |
| --- | --- | --- |
| Provider | One approved provider per storage boundary | Do not select it from an untrusted request or silently fall back to disk/in-memory. |
| Root/bucket/container/share | Authorized logical destination | Validate provider naming rules and fail startup when absent. |
| Key prefix | Stable tenant/workload prefix | Validate each logical segment; normalization does not remove `.` or `..`. |
| Maximum object/list size | Workload-specific hard limit | Bound materialized reads and set `MaxResults` for every non-administrative listing. |
| Retry/timeout budget | One documented owner and total deadline | Account for native SDK and provider retries before adding an outer policy. |

## Enterprise implementation guidance

- Keep credentials out of connection strings, source, logs, and exception telemetry. Select a provider constructor that accepts an existing native client or an identity-based credential where available; resolve secrets through the platform secret manager only when a long-lived secret is unavoidable.
- Assign least-privilege access to the specific bucket/container/share and prefix where the platform supports it. Apply network restrictions, private endpoints, TLS, and key rotation in infrastructure.
- Define write idempotency before adding retries. A retry after an unknown write outcome can overwrite an object or produce a duplicate external effect. Use deterministic object keys, conditional writes/version preconditions offered by the native provider, and record an application operation ID.
- Native SDKs may already retry transient calls. Configure one retry owner and budget; do not blindly layer Polly or `Microsoft.Extensions.Resilience` around every `IStore` call. If an outer policy is required, retry only documented transient failures and idempotent operations, keep the attempt/timeout budget small, and emit attempt telemetry.
- FluentStorage itself does not provide a uniform OpenTelemetry instrumentation contract. Capture provider request IDs, status, bucket/container/share, sanitized key prefix, operation, bytes, duration, retry count, and correlation ID without logging object contents, credentials, SAS tokens, or full sensitive keys.

### Upgrade and rollback

Upgrade `FluentStorage` with every selected `FluentStorage.*` provider as one compatibility set. Before merging, restore the exact graph, compile each used factory, and run provider integration tests for path normalization, overwrite/append capability, listing limits, stream disposal, and cancellation. Recheck transitive native SDK versions and any new trimming warnings; a provider package can change behavior even when the shared `IStore` surface still compiles.

For rollback, retain the previously deployed package graph and configuration, stop new transfers, drain in-flight work, and redeploy the last verified core/provider set. Do not roll back by switching production to disk/in-memory or another provider. Storage writes made during the failed release are external state: reconcile deterministic keys and provider versions/generations before replaying work.

## Integration with the catalog

- Providers: [AWS](fluentstorage-aws.md), [Azure Blobs](fluentstorage-azure-blobs.md), [Azure Files](fluentstorage-azure-files.md), [GCP](fluentstorage-gcp.md), [MinIO](fluentstorage-minio.md), and [SFTP](fluentstorage-sftp.md).
- Resilience policy must be designed with the catalog’s Polly and Microsoft resilience entries; do not enable a second unbounded retry layer around an SDK retry policy.
- Use the repository OpenTelemetry guidance for host-wide tracing and metrics; this package supplies no provider-neutral instrumentation configuration.
- Selection boundary: [Storage abstraction and provider SDKs](../package-guidance/package-selection.md#storage-abstraction-and-provider-sdks).
- End-to-end workflow: [Portable storage upload and download](../recipes/fluentstorage-portable-transfer.md).
- Provenance and dependency review: [FluentStorage supply-chain entry](../package-guidance/supply-chain.md#fluentstorage).

## Security, performance, AOT, trimming, and operations

The default write methods overwrite when `append` is `false`. Do not assume append is portable: S3 and Google Cloud Storage reject it, while filesystem-style providers can implement it. Do not use `ObjectExists` followed by `SetObject` as an atomic create operation; it has a race. Prefer provider-native conditional semantics when correctness needs them.

The abstraction does not claim Native AOT or trimming support. The approved compatibility/AOT boundary is the application’s explicitly tested subset, not this provider family as a whole. Keep runtime provider discovery, connection-string module registration, and reflection-sensitive third-party SDK paths outside Native-AOT-critical executables unless published and tested for that deployment target.

### Operational signals

Measure operation count, failures and latency by provider and operation; transferred bytes; active transfers; bounded-list result count; cancellations; retries; and unknown write outcomes. Alert on sustained failure/latency increases, growing active-transfer backlogs, repeated full-limit listings, disk capacity/inode pressure for the disk store, and reconciliation backlog. Log only a sanitized destination and key prefix plus provider request/correlation IDs—never payloads, credentials, tokens, signed URLs, or sensitive full keys.

### Troubleshooting

| Symptom | Likely cause | Diagnostic | Correction | Retry? |
| --- | --- | --- | --- | --- |
| Object cannot be found under the expected key | Separator normalization, wrong root/prefix, or unvalidated logical segments | Record the sanitized normalized key and configured destination; compare with a bounded provider listing | Construct keys from validated segments and correct the authoritative root/prefix | Only after configuration is corrected; not as a transient retry |
| Memory or latency spikes on reads/listing | `GetBytes`/`GetText` materialized a large object or listing was insufficiently bounded | Inspect object size, allocation telemetry, `FolderPath`, `MaxResults`, and result count | Stream content and enforce size/list limits | No; retrying repeats the resource pressure |
| Timed-out write has an uncertain result | Network failure occurred after the provider accepted some/all data | Query the deterministic key and native version/generation/checksum without issuing another write | Reconcile, then resume or replace using provider-native conditions | Only when idempotency is proved and within the single retry budget |
| Disk store fails with access or capacity errors | Missing root, process permissions, full filesystem, or exhausted inodes | Check the configured root, service identity, free space/inodes, and OS error | Provision the root/permissions/capacity; keep disk use explicitly local | Retry only transient I/O failures after the condition clears |

## Avoid

- Do not use `FromConnectionString` with credentials in checked-in configuration. It requires provider module registration and serializes secrets in a format that is easy to leak.
- Do not model an object prefix as a transactional directory. Listing, delete, and recursive operations have provider-specific consistency, pagination, and cost characteristics.
- Do not use `GetClient()` from business code to escape the abstraction without documenting the resulting provider dependency.
- Do not make generic `IStore` calls the only storage contract when a workload depends on provider features such as object lock, lifecycle policy, tags, metadata, access tiers, or conditional version writes.

## Verification checklist

- [ ] Restore resolves `FluentStorage` at `8.0.16` without a downgrade or provider/core version conflict.
- [ ] A local/in-memory test covers key normalization, overwrite behavior, stream disposal, cancellation, and a bounded read.
- [ ] Production configuration constructs one provider store from secret-managed or workload identity inputs and never logs credentials.
- [ ] The production provider’s idempotency, retry, encryption, network, retention, and observability requirements have been tested against that provider.
- [ ] Any trimming or Native AOT claim is backed by a publish-and-run test of the exact application path.

## Sources

Accessed 2026-07-27.

- [FluentStorage upstream README](https://github.com/robinrodricks/FluentStorage)
- [FluentStorage `IStore` contract](https://github.com/robinrodricks/FluentStorage/blob/develop/FluentStorage/Storage/IStore.cs)
- [FluentStorage listing options](https://github.com/robinrodricks/FluentStorage/blob/develop/FluentStorage/Model/StorageListOptions.cs)
- [FluentStorage 8.0.16 on NuGet](https://www.nuget.org/packages/FluentStorage/8.0.16)
