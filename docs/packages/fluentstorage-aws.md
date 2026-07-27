# FluentStorage.AWS

## Catalog entry

| Field | Value |
| --- | --- |
| Package | `FluentStorage.AWS` |
| Pinned version | `8.0.10` |
| Role | Amazon S3 implementation of FluentStorage `IStore` |
| Status | Approved for S3 workloads that fit the shared contract; retain native S3 APIs for S3-specific controls |
| Owner | IX |
| Last reviewed | 2026-07-27 |
| Review trigger | Any `FluentStorage.AWS`, AWS SDK, target-framework, S3 endpoint, credential-chain, retry-default, or bucket-policy change |

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

| Setting | Required/typical value | Operational note |
| --- | --- | --- |
| Bucket | Pre-provisioned bucket name | Fail startup on a missing/mistyped bucket; never accept it from a request. |
| Region | Exact bucket region, for example `eu-central-1` | A mismatch can produce redirects, signing failures, or extra latency. |
| Credentials | Role/federated SDK chain | Avoid static keys; verify the resolved caller identity in deployment diagnostics without logging credentials. |
| Service URL/path style | AWS default unless an approved compatible endpoint requires overrides | Keep endpoint, TLS, authentication region, and path-style behavior together and integration-test them. |
| Transfer/retry budget | Bounded SDK attempts, timeouts, and tested multipart thresholds | Treat the AWS SDK as retry owner and reconcile unknown write outcomes. |

## Enterprise implementation guidance

- Use IAM roles and the AWS SDK credential provider chain; scope the role to bucket and prefix actions required by the workload. Do not distribute long-lived access keys.
- Enforce encryption at rest with an S3 bucket policy and the selected SSE mode (SSE-S3 or SSE-KMS). Use KMS key policy and grants for SSE-KMS; FluentStorage does not expose a uniform per-write encryption choice.
- AWS SDK for .NET has its own retry configuration. Set a bounded SDK retry/timeout budget first. Add Polly or Microsoft resilience only around idempotent, failure-classified application operations, never as an unconditional second retry loop.
- Emit sanitized operation/key-prefix telemetry and correlate AWS request IDs. Do not record authorization headers, access keys, session tokens, presigned URLs, or object content.

### Upgrade and rollback

Upgrade the provider, core package, and resolved AWS SDK graph together in a staging branch. Compile the exact `FromRole`/credential/client construction used by the application, inspect dependency changes, and integration-test region resolution, IAM/KMS authorization, overwrite, rejected append, large/non-seekable transfers, listing pagination, retries, and cancellation against a non-production bucket.

Rollback by restoring the previously verified package graph and AWS configuration; do not change bucket, region, key prefix, or encryption policy as part of the rollback. Drain transfers first and reconcile timed-out writes by deterministic key plus S3 version ID/checksum where enabled. Multipart uploads and objects already created remain external state and may require explicit lifecycle cleanup or reconciliation.

## Integration with the catalog

- Shared stream/path/retry guidance: [FluentStorage](fluentstorage.md).
- S3-compatible but independently configured option: [FluentStorage.Minio](fluentstorage-minio.md).
- Coordinate application-level resilience with the catalog’s Polly/Microsoft resilience documentation; retain AWS SDK retries as the provider-level owner.
- Selection boundary: [Storage abstraction and provider SDKs](../package-guidance/package-selection.md#storage-abstraction-and-provider-sdks).
- End-to-end workflow: [Portable storage upload and download](../recipes/fluentstorage-portable-transfer.md).
- Provenance and dependency review: [FluentStorage.AWS supply-chain entry](../package-guidance/supply-chain.md#fluentstorage-aws).

## Security, performance, AOT, trimming, and operations

S3 object versioning, tags, and storage tiers are bucket/object capabilities that must be enabled and tested in the account; `IStore` capability methods report support but do not configure them. Listing is prefix-based and paginated, can be expensive at scale, and is not a directory transaction. Lifecycle, replication, retention, Object Lock, inventory, and server-side encryption belong in bucket provisioning.

For objects around 100 MB or larger, AWS recommends considering multipart upload. Validate FluentStorage's transfer behavior for the pinned provider; when explicit part size, concurrency, checksum, abort, or resume control is required, use the native S3 transfer APIs behind a provider-specific adapter. Configure a lifecycle rule to abort incomplete multipart uploads so abandoned parts do not accumulate charges.

Do not implement “create only if absent” with `ObjectExists` then write. It races. For write-once or version-sensitive workflows, use a native S3 conditional/version-aware operation behind an application-owned interface. A retry after a network timeout may leave the write outcome unknown; deterministic keys and reconciliation are required.

This provider is not documented as trimming- or Native-AOT-safe. Validate the precise AWS/FluentStorage path in a publish-and-run gate before asserting either property.

### Operational signals

Measure calls, latency, bytes, retries, throttles and failures by S3 operation; active/multipart uploads; bounded-list result counts; and unknown write outcomes. Correlate the AWS request ID and extended request ID with sanitized bucket/key-prefix telemetry. Alert on sustained `5xx`/`SlowDown`, `403`, latency, incomplete-multipart growth, or reconciliation backlog; never record credentials, authorization headers, presigned URLs, payloads, or sensitive full keys.

### Troubleshooting

| Symptom | Likely cause | Diagnostic | Correction | Retry? |
| --- | --- | --- | --- | --- |
| `403 AccessDenied` | IAM/bucket/KMS/VPC endpoint policy denial or wrong principal | Capture request IDs and error code; verify resolved identity, action/resource policy, bucket owner, and KMS grant | Correct the least-privilege policy or credential source | No; retry only after authorization changes propagate |
| Redirect/signature/endpoint error | Bucket region, service URL, auth region, clock, or path-style mismatch | Compare configured region/endpoint with bucket location and inspect the SDK error code/request ID | Correct region/endpoint/TLS/path-style configuration | Only after correction |
| `503 SlowDown` or high throttling | Request-rate hotspot or service throttling | Inspect SDK attempt metrics, S3 request metrics, key distribution, and request IDs | Use SDK backoff, distribute workload, and keep concurrency within the measured budget | Yes, bounded SDK retry with jitter for idempotent calls |
| Timed-out upload has unknown outcome | Response was lost after S3 accepted the write or multipart work remains | `HEAD` the deterministic key/version and compare checksum/metadata; inspect multipart uploads | Reconcile the object; abort stale multipart uploads through lifecycle/native tooling | Only after reconciliation proves replay is safe |

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
- [Troubleshoot S3 `403 AccessDenied`](https://docs.aws.amazon.com/AmazonS3/latest/userguide/troubleshoot-403-errors.html)
- [Amazon S3 performance design patterns](https://docs.aws.amazon.com/AmazonS3/latest/userguide/optimizing-performance-design-patterns.html)
