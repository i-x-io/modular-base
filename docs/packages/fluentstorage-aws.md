# FluentStorage.AWS

## Catalog entry

| Field | Value |
| --- | --- |
| Package | `FluentStorage.AWS` |
| Pinned version | `8.0.10` |
| Role | Amazon S3 implementation of FluentStorage `IStore` |
| Status | Approved for S3 workloads that fit the shared contract; retain native S3 APIs for S3-specific controls |

## Decision and scope

Use this provider for an S3 bucket selected at application composition time. An S3 key is an opaque object key; slash-separated “folders” are prefixes, not directories. S3 does not support append, and this provider throws for `append: true`.

Prefer the native AWS SDK boundary when the workload needs Object Lock, legal holds, replication, inventory, access points, multipart tuning, presigned URLs, KMS encryption headers, or conditional/version-specific operations beyond FluentStorage’s API.

## Recommended registration and use

Reference the provider without a version because the catalog pins `8.0.10` centrally:

```xml
<ItemGroup>
  <PackageReference Include="FluentStorage.AWS" />
</ItemGroup>
```

For workloads running on AWS, prefer the identity-based role constructor rather than static access keys:

```csharp
using FluentStorage;
using FluentStorage.Storage;

IStore store = AwsS3Storage.FromRole(
    bucketName: "modular-base-documents",
    region: "eu-central-1");
```

`FromRole` is suitable when the AWS SDK can obtain role credentials from the workload environment. For local development, use a named profile or an explicitly supplied short-lived credential through a secret-managed configuration boundary—never literals. Do not use a connection string for credential-bearing production configuration.

Before startup, provision the bucket and region, attach an IAM role, and grant only the required `s3:GetObject`, `s3:PutObject`, `s3:DeleteObject`, and `s3:ListBucket` actions (plus KMS permissions when applicable). Fail startup when the bucket or region is missing; do not silently switch buckets. After construction, use the upload/download/list/delete workflow in [FluentStorage](fluentstorage.md). For S3 specifically, keep listings prefix-bounded and set `MaxResults`; deleting a “directory” means listing and deleting every matching key, not removing one atomic resource.

Use stable keys and stream data through `SetObject`/`OpenRead`; the AWS SDK may buffer non-seekable input for upload. Keep upload size, buffering, cancellation, and multipart behavior under load test. Dispose returned read/write streams and the store when its host lifetime ends.

## Enterprise implementation guidance

- Use IAM roles and the AWS SDK credential provider chain; scope the role to bucket and prefix actions required by the workload. Do not distribute long-lived access keys.
- Enforce encryption at rest with an S3 bucket policy and the selected SSE mode (SSE-S3 or SSE-KMS). Use KMS key policy and grants for SSE-KMS; FluentStorage does not expose a uniform per-write encryption choice.
- AWS SDK for .NET has its own retry configuration. Set a bounded SDK retry/timeout budget first. Add Polly or Microsoft resilience only around idempotent, failure-classified application operations, never as an unconditional second retry loop.
- Emit sanitized operation/key-prefix telemetry and correlate AWS request IDs. Do not record authorization headers, access keys, session tokens, presigned URLs, or object content.

## Integration with the catalog

- Shared stream/path/retry guidance: [FluentStorage](fluentstorage.md).
- S3-compatible but independently configured option: [FluentStorage.Minio](fluentstorage-minio.md).
- Coordinate application-level resilience with the catalog’s Polly/Microsoft resilience documentation; retain AWS SDK retries as the provider-level owner.

## Security, performance, AOT, trimming, and operations

S3 object versioning, tags, and storage tiers are bucket/object capabilities that must be enabled and tested in the account; `IStore` capability methods report support but do not configure them. Listing is prefix-based and paginated, can be expensive at scale, and is not a directory transaction. Lifecycle, replication, retention, Object Lock, inventory, and server-side encryption belong in bucket provisioning.

For objects around 100 MB or larger, AWS recommends considering multipart upload. Validate FluentStorage's transfer behavior for the pinned provider; when explicit part size, concurrency, checksum, abort, or resume control is required, use the native S3 transfer APIs behind a provider-specific adapter. Configure a lifecycle rule to abort incomplete multipart uploads so abandoned parts do not accumulate charges.

Do not implement “create only if absent” with `ObjectExists` then write. It races. For write-once or version-sensitive workflows, use a native S3 conditional/version-aware operation behind an application-owned interface. A retry after a network timeout may leave the write outcome unknown; deterministic keys and reconciliation are required.

This provider is not documented as trimming- or Native-AOT-safe. Validate the precise AWS/FluentStorage path in a publish-and-run gate before asserting either property.

## Avoid

- Do not set `append: true`; S3 has no append operation.
- Do not treat prefixes as directories or expect recursive deletion/listing to be atomic.
- Do not use static credentials in source, `appsettings*.json`, connection strings, or logs.
- Do not assume a generic `IStore` write configures SSE-KMS, Object Lock, or lifecycle policy.

## Verification checklist

- [ ] Restore resolves `FluentStorage.AWS` `8.0.10` and the catalog core version without downgrade warnings.
- [ ] An IAM role or federated workload identity can list/read/write only the intended bucket/prefix.
- [ ] A test proves overwrite, failed append, stream disposal, cancellation, and unknown-outcome reconciliation for the chosen key scheme.
- [ ] Bucket policy enforces TLS and required server-side encryption; KMS access is least-privilege where used.
- [ ] Load tests cover non-seekable/large-stream uploads and the configured SDK retry/timeout budget.

## Sources

Accessed 2026-07-27.

- [FluentStorage AWS factory/source](https://github.com/robinrodricks/FluentStorage/tree/develop/FluentStorage.AWS)
- [FluentStorage.AWS 8.0.10 on NuGet](https://www.nuget.org/packages/FluentStorage.AWS/8.0.10)
- [AWS SDK for .NET retries and timeouts](https://docs.aws.amazon.com/sdk-for-net/v4/developer-guide/retries-timeouts.html)
- [AWS SDK credential and profile resolution](https://docs.aws.amazon.com/sdk-for-net/v4/developer-guide/creds-assign.html)
- [Amazon S3 multipart upload overview](https://docs.aws.amazon.com/AmazonS3/latest/userguide/mpuoverview.html)
- [Amazon S3 server-side encryption](https://docs.aws.amazon.com/AmazonS3/latest/userguide/serv-side-encryption.html)
