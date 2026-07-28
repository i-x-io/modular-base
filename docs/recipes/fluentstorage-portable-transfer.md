# Portable upload and download with FluentStorage

## Problem and boundary

Use this recipe when application code needs a small upload/download boundary that can be backed by one approved FluentStorage provider chosen at deployment. The composition root owns provider selection, credentials, root/container provisioning, retry policy, and store disposal. `IStore` owns the common transfer calls. Provider-specific guarantees such as conditional creation, version IDs, legal holds, leases, access tiers, atomic rename, and retention remain outside this portable boundary.

## Required catalog packages

A reusable storage library can reference the centrally managed core package:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>14.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="FluentStorage" />
  </ItemGroup>
</Project>
```

Add exactly one approved `FluentStorage.*` provider reference in the infrastructure/composition project when production does not use the core disk implementation. Keep provider packages out of the application service so a backend change affects composition and integration tests, not business code.

## Select the provider at composition

```csharp
using FluentStorage;
using FluentStorage.Storage;

public sealed record StorageSettings(string Provider, string Root);

public static class StoreComposition
{
    public static IStore Create(StorageSettings settings) => settings.Provider switch
    {
        "Disk" when Path.IsPathFullyQualified(settings.Root) =>
            StorageFactory.Disk(settings.Root),
        "Disk" => throw new InvalidOperationException(
            "Storage:Root must be an absolute path."),
        _ => throw new InvalidOperationException(
            $"Storage provider '{settings.Provider}' is not configured.")
    };
}
```

Provider choice comes only from trusted, startup-validated deployment configuration; it must never come from an upload request. This locally compilable factory intentionally supports only disk. A production composition project replaces or extends the switch with the selected catalog provider factory and its identity-based native client. Fail startup for an unsupported provider—never silently fall back to disk or in-memory storage. The host owns the resulting long-lived `IStore` and disposes it only after in-flight transfers stop.

## Validate logical object keys

```csharp
public static class ObjectKeys
{
    public static string Document(string tenantId, Guid documentId, string extension)
    {
        string tenant = Segment(tenantId, nameof(tenantId));
        string suffix = Segment(extension.TrimStart('.'), nameof(extension));
        return $"tenant/{tenant}/document/{documentId:N}.{suffix}";
    }

    private static string Segment(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value is "." or ".." ||
            value.Contains('/') ||
            value.Contains('\\'))
        {
            throw new ArgumentException(
                "Object-key segments must be non-empty and cannot contain separators.",
                parameterName);
        }

        return value;
    }
}
```

The application creates deterministic object identifiers from validated logical segments. An object key is not an operating-system path, and FluentStorage separator normalization is not input sanitization. Apply workload-specific character, length, tenant-authorization, and extension/media-type rules before this helper. Do not expose full sensitive keys in logs or metrics.

## Stream uploads and downloads

```csharp
using FluentStorage.Storage;

public sealed class DocumentTransfer(IStore store)
{
    public async Task UploadAsync(
        string key,
        Stream source,
        string contentType,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        await store.SetObject(
            key,
            source,
            contentType,
            append: false,
            cancellationToken);
    }

    public async Task DownloadAsync(
        string key,
        Stream destination,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        using Stream source = await store.OpenRead(key, cancellationToken);
        await source.CopyToAsync(destination, cancellationToken);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken) =>
        store.DeleteObject(key, cancellationToken);
}
```

`SetObject` completes before the caller may dispose or reuse the upload stream; this service does not take ownership of caller-provided streams. `OpenRead` returns a stream owned and disposed by the service, while the destination remains caller-owned. Streaming avoids the unbounded allocations of `GetBytes` and `GetText`. Enforce request and object size limits outside this class, authorize the tenant/key before every call, and scan untrusted uploads before making them available.

An overwrite with the same key is not a portable create-if-absent or compare-and-swap operation. When duplicate prevention, immutability, resumable multipart upload, checksums, version-specific reads, or conditional deletion is required, expose that requirement in an application-owned interface and implement it with the selected provider's native SDK.

## Compose a local harness

```csharp
string root = Path.Combine(Path.GetTempPath(), "catalog-storage-recipe");
Directory.CreateDirectory(root);

using IStore store = StoreComposition.Create(new StorageSettings("Disk", root));
var transfer = new DocumentTransfer(store);
string key = ObjectKeys.Document("acme", Guid.NewGuid(), "txt");

await using var upload = new MemoryStream("portable payload"u8.ToArray());
await transfer.UploadAsync(key, upload, "text/plain", CancellationToken.None);

await using var download = new MemoryStream();
await transfer.DownloadAsync(key, download, CancellationToken.None);

if (!download.ToArray().AsSpan().SequenceEqual("portable payload"u8))
{
    throw new InvalidOperationException("Downloaded bytes differ from uploaded bytes.");
}

await transfer.DeleteAsync(key, CancellationToken.None);
```

The harness creates and owns a disk store, uploads a bounded stream, downloads into a caller-owned stream, compares bytes, deletes the object, then disposes the store. It validates the common local workflow only. It says nothing about production identity, durability, concurrency, consistency, retry defaults, conditional writes, pagination, network failure, or provider-specific stream behavior.

## Failure modes and operations

| Signal or symptom | Interpretation | Action |
| --- | --- | --- |
| Startup rejects provider/root | Deployment configuration is absent or unsupported | Correct configuration; do not enable an implicit fallback |
| Upload fails or times out | Authorization, quota, network, provider, or local capacity failure; final write state may be unknown | Inspect provider request ID and native diagnostics; reconcile before retrying a non-idempotent key |
| Download is missing or forbidden | Wrong key/prefix, lifecycle deletion, permissions, or replication/consistency behavior | Verify authorized canonical key and provider audit events without logging object data |
| Memory or latency rises | A caller materializes large content, buffers excessively, or saturates connections | Stream, enforce byte/concurrency limits, and measure transfer size/duration |
| Provider swap changes behavior | The shared API did not erase backend semantics | Run the provider contract suite and adapt the infrastructure boundary |

Observe operation, outcome, duration, bytes, bounded key prefix, provider, retry count, and native request ID. Never log object contents, credentials, signed URLs, SAS tokens, connection strings, or full keys containing sensitive identifiers. Let the native provider SDK own retries unless the integration explicitly establishes a single different retry owner and a total deadline.

## Verification checklist

Authoring verification for this recipe:

- [x] The provider-neutral service and local disk harness were compiled and run in a temporary `net10.0` project against the cataloged `FluentStorage` version.
- [x] The authoring check used only a temporary local directory; no cloud or remote provider was contacted.

Checks for the consuming application:

- [ ] Validate startup provider selection, workload identity, least privilege, network policy, encryption, and root/container provisioning.
- [ ] Run the same upload/download/delete contract against the selected provider, including zero-byte and maximum-size objects, cancellation, timeout, overwrite, and unknown write outcomes.
- [ ] Test key authorization and traversal-like segments; confirm logs and traces redact sensitive identifiers and credentials.
- [ ] Verify native conditional/versioned operations for every workflow that needs stronger semantics than `IStore` exposes.
- [ ] Exercise graceful shutdown so stores are disposed only after in-flight streams complete.

## Related package guides

- [FluentStorage](../packages/fluentstorage.md)
- [FluentStorage.AWS](../packages/fluentstorage-aws.md)
- [FluentStorage.Azure.Blobs](../packages/fluentstorage-azure-blobs.md)
- [FluentStorage.Azure.Files](../packages/fluentstorage-azure-files.md)
- [FluentStorage.GCP](../packages/fluentstorage-gcp.md)
- [FluentStorage.Minio](../packages/fluentstorage-minio.md)
- [FluentStorage.SFTP](../packages/fluentstorage-sftp.md)

## Primary sources

- [FluentStorage 8.0.16 on NuGet](https://www.nuget.org/packages/FluentStorage/8.0.16) — Accessed 2026-07-27.
- [FluentStorage upstream README](https://github.com/robinrodricks/FluentStorage) — Accessed 2026-07-27.
- [FluentStorage `IStore` contract](https://github.com/robinrodricks/FluentStorage/blob/develop/FluentStorage/Storage/IStore.cs) — Accessed 2026-07-27.
- [FluentStorage disk implementation](https://github.com/robinrodricks/FluentStorage/tree/develop/FluentStorage/Files) — Accessed 2026-07-27.
