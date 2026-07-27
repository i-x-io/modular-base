# FluentStorage.Minio

## Catalog entry

| Field | Value |
| --- | --- |
| Package | `FluentStorage.Minio` |
| Pinned version | `8.0.10` |
| Role | MinIO implementation of FluentStorage `IStore` |
| Status | Approved for a deliberately operated MinIO/S3-compatible deployment; not a substitute for validating the target service’s semantics |
| Owner | IX |
| Last reviewed | 2026-07-27 |
| Review trigger | Any provider/MinIO client, target-framework, server release, endpoint/TLS, identity/STS, retry, erasure-set, or bucket-policy change |

## Decision and scope

Use this provider for MinIO endpoints and buckets, including deployments that need MinIO STS/IAM role flows. It is S3-compatible object storage, not a filesystem: bucket objects are keys and slash prefixes are virtual. Its behavior, observability, encryption, lifecycle, replication, and IAM capabilities are defined by the deployed MinIO version and configuration.

## Recommended registration and use

Reference the provider without a version because the catalog pins `8.0.10` centrally:

```xml
<ItemGroup>
  <PackageReference Include="FluentStorage.Minio" />
</ItemGroup>
```

For static credentials only when a secret manager injects them at the composition root:

```csharp
using FluentStorage;
using FluentStorage.Storage;

IStore store = MinioStorage.FromCredentials(
    endpoint: "minio.internal.example",
    accessKey: Environment.GetEnvironmentVariable("MINIO_ACCESS_KEY")!,
    secretKey: Environment.GetEnvironmentVariable("MINIO_SECRET_KEY")!,
    bucketName: "documents",
    useSsl: true);
```

Prefer `FromIamRole`, `FromSts`, `FromAssumeRole`, or `FromClient` when the deployment supports temporary/federated credentials or the MinIO client must carry explicit transport and observability configuration. Do not store access/secret keys in checked-in settings, connection strings, or logs. Dispose returned streams and keep large transfers streaming.

Provision the bucket, TLS certificate trust, DNS endpoint, IAM policy, lifecycle, versioning, and quotas before application startup. Health checks should distinguish DNS/TLS reachability from authorization and bucket access without uploading a new probe object on every check. After construction, use the shared upload/download/list/delete workflow from [FluentStorage](fluentstorage.md). Set a narrow `FolderPath`, `MaxResults`, and a deliberate recursion mode; deleting a prefix requires enumeration and multiple object deletes. Use the native MinIO client behind an application adapter for conditional/version-specific writes, multipart controls, presigned URLs, retention, or object lock.

| Setting | Required/typical value | Operational note |
| --- | --- | --- |
| Endpoint | Exact DNS name/port with `useSsl: true` | Validate certificate SAN/chain and load-balancer routing; never silently downgrade TLS. |
| Bucket | Pre-provisioned authorized bucket | Fail startup on missing/mistyped bucket; keep tenant routing out of request-controlled bucket selection. |
| Credential | IAM role/STS/client configuration preferred | Scope temporary/static credentials to the workload prefix and rotate static keys. |
| Region/path behavior | Values required by the deployed MinIO gateway/server | Test signing, redirects and compatibility against the exact release and proxy. |
| Retry/transfer budget | One bounded owner, timeouts, concurrency and multipart limits | Include server/load-balancer timeouts and reconcile unknown uploads. |

## Enterprise implementation guidance

- Require TLS (`useSsl: true`) and validate certificates. Use per-workload IAM policies scoped to the intended bucket/prefix; rotate static keys where they cannot be eliminated.
- Configure server-side encryption, KMS integration, object locking/retention, versioning, lifecycle, replication, audit logs, quotas, and network access in MinIO. The generic `IStore` contract does not set these policy controls.
- The MinIO SDK has its own transport and retry behavior. Establish one bounded retry/timeout owner; only add a Polly/Microsoft resilience policy for classified transient and idempotent operations. Deterministic keys and reconciliation are needed after unknown write outcomes.
- Telemetry should include sanitized endpoint/bucket/key-prefix, operation, bytes, duration and status/request correlation. Never emit credentials, presigned URLs, encryption keys, or sensitive names.

### Upgrade and rollback

Treat the FluentStorage provider, resolved MinIO client, and deployed MinIO server/configuration as a compatibility matrix. Before upgrading, compile the actual role/STS/client factory and test TLS trust, signing, IAM, overwrite/list/delete, large multipart streams, cancellation, retry attempts, object version/retention interactions, and rolling-node behavior against a staging cluster on the target server release.

Rollback the client package graph independently only when the server/API remains compatible; otherwise follow the operator’s tested server rollback procedure. Preserve endpoint, bucket, prefix, credentials, encryption and retention configuration. Drain transfers and reconcile timed-out writes/version IDs/checksums plus incomplete multipart uploads. Objects and cluster metadata created by the failed release remain external state and must not be deleted blindly.

## Integration with the catalog

- Shared storage semantics and stream guidance: [FluentStorage](fluentstorage.md).
- AWS S3 provider: [FluentStorage.AWS](fluentstorage-aws.md).
- Apply the catalog Polly/Microsoft resilience patterns only after provider SDK retries and application idempotency are defined.
- Selection boundary: [Storage abstraction and provider SDKs](../package-guidance/package-selection.md#storage-abstraction-and-provider-sdks).
- End-to-end workflow: [Portable storage upload and download](../recipes/fluentstorage-portable-transfer.md).
- Provenance and dependency review: [FluentStorage.Minio supply-chain entry](../package-guidance/supply-chain.md#fluentstorage-minio).

## Security, performance, AOT, trimming, and operations

MinIO exposes object-store semantics and may differ from AWS S3 in API availability, identity integration, administration, or consistency/capacity behavior. Test against the exact deployed MinIO release and configuration. Do not expect `IStore` to expose S3/MinIO object locking, conditional writes, version IDs, multipart tuning, tags, or encryption headers uniformly.

Do not create atomic workflows from `ObjectExists` followed by write. Use native conditional/version semantics in a provider-specific application adapter when required. This package has no documented trimming/Native-AOT guarantee; test the actual client/provider path before claiming support.

### Operational signals

Measure request count/latency/bytes, SDK attempts, error codes, active/multipart transfers, bounded-list counts, capacity/quota, node/disk/erasure-set health, replication lag and unknown outcomes. Correlate server audit/request identifiers with sanitized endpoint/bucket/prefix. Alert on sustained auth failures, `SlowDown`/`5xx`, latency, healing/degraded sets, capacity pressure, replication lag, incomplete multipart growth, or reconciliation backlog; exclude credentials, signed URLs, encryption material, payloads, and sensitive names.

### Troubleshooting

| Symptom | Likely cause | Diagnostic | Correction | Retry? |
| --- | --- | --- | --- | --- |
| TLS/DNS/signature failure | Wrong endpoint/certificate trust, proxy rewriting, clock/region/path mismatch | Check DNS, certificate chain/SAN, clock, endpoint/region, proxy logs and server request/audit record | Correct endpoint, trust, clock or proxy/signing configuration; keep TLS enabled | Only after correction |
| `AccessDenied`/authentication failure | Wrong credential source, expired STS token, or IAM/bucket/KMS denial | Identify resolved principal and error code; review scoped policy and audit log without logging secrets | Refresh temporary identity or correct least-privilege IAM/KMS policy | No; retry after correction/refresh only |
| `SlowDown`/`5xx` or high latency | Load balancer timeout, capacity pressure, degraded/healing cluster, or throttling | Inspect client attempts plus MinIO node/disk/erasure-set/capacity metrics and audit request | Restore cluster health/capacity, bound concurrency and let one retry owner back off | Yes for idempotent calls after confirming cluster can accept load |
| Timed-out upload has unknown outcome | Response loss or incomplete multipart upload | Query deterministic key/version/checksum and list incomplete multipart work with native tooling | Reconcile object; abort stale multipart uploads under an audited lifecycle/process | Only after reconciliation proves replay safe |

## Avoid

- Do not disable TLS outside a deliberately isolated local-development environment.
- Do not assume MinIO and AWS S3 operational/security semantics are identical because both accept object keys.
- Do not expose bucket/key selection directly from untrusted input.
- Do not use generic operations as a replacement for version locking, KMS policy, retention, or lifecycle administration.

## Verification checklist

- [ ] Restore resolves `FluentStorage.Minio` `8.0.10` without a core-package downgrade.
- [ ] TLS/certificate validation, IAM policy, secret rotation or federated credentials, and endpoint network reachability are verified against the deployed cluster.
- [ ] Bucket encryption, versioning/retention, lifecycle/replication, audit logging, and capacity/quota controls meet workload policy.
- [ ] Tests cover streaming disposal, cancellation, overwrite/idempotency, unknown-write reconciliation, and target-version interoperability.
- [ ] A publish-and-run test supports any trim/AOT statement.

## Sources

Accessed 2026-07-27.

- [FluentStorage MinIO factory/source](https://github.com/robinrodricks/FluentStorage/tree/develop/FluentStorage.Minio)
- [FluentStorage.Minio 8.0.10 on NuGet](https://www.nuget.org/packages/FluentStorage.Minio/8.0.10)
- [MinIO .NET SDK documentation](https://docs.min.io/enterprise/aistor-object-store/developers/sdk/dotnet/api/)
- [MinIO server-side encryption](https://docs.min.io/enterprise/aistor-object-store/installation/kubernetes/server-side-encryption/)
- [MinIO IAM policy documentation](https://docs.min.io/enterprise/aistor-object-store/administration/iam/access/)
