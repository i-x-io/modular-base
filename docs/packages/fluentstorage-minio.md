# FluentStorage.Minio

## Catalog entry

| Field | Value |
| --- | --- |
| Package | `FluentStorage.Minio` |
| Pinned version | `8.0.10` |
| Role | MinIO implementation of FluentStorage `IStore` |
| Status | Approved for a deliberately operated MinIO/S3-compatible deployment; not a substitute for validating the target service’s semantics |

## Decision and scope

Use this provider for MinIO endpoints and buckets, including deployments that need MinIO STS/IAM role flows. It is S3-compatible object storage, not a filesystem: bucket objects are keys and slash prefixes are virtual. Its behavior, observability, encryption, lifecycle, replication, and IAM capabilities are defined by the deployed MinIO version and configuration.

## Recommended registration and use

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

## Enterprise implementation guidance

- Require TLS (`useSsl: true`) and validate certificates. Use per-workload IAM policies scoped to the intended bucket/prefix; rotate static keys where they cannot be eliminated.
- Configure server-side encryption, KMS integration, object locking/retention, versioning, lifecycle, replication, audit logs, quotas, and network access in MinIO. The generic `IStore` contract does not set these policy controls.
- The MinIO SDK has its own transport and retry behavior. Establish one bounded retry/timeout owner; only add a Polly/Microsoft resilience policy for classified transient and idempotent operations. Deterministic keys and reconciliation are needed after unknown write outcomes.
- Telemetry should include sanitized endpoint/bucket/key-prefix, operation, bytes, duration and status/request correlation. Never emit credentials, presigned URLs, encryption keys, or sensitive names.

## Integration with the catalog

- Shared storage semantics and stream guidance: [FluentStorage](fluentstorage.md).
- AWS S3 provider: [FluentStorage.AWS](fluentstorage-aws.md).
- Apply the catalog Polly/Microsoft resilience patterns only after provider SDK retries and application idempotency are defined.

## Security, performance, AOT, trimming, and operations

MinIO exposes object-store semantics and may differ from AWS S3 in API availability, identity integration, administration, or consistency/capacity behavior. Test against the exact deployed MinIO release and configuration. Do not expect `IStore` to expose S3/MinIO object locking, conditional writes, version IDs, multipart tuning, tags, or encryption headers uniformly.

Do not create atomic workflows from `ObjectExists` followed by write. Use native conditional/version semantics in a provider-specific application adapter when required. This package has no documented trimming/Native-AOT guarantee; test the actual client/provider path before claiming support.

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
- [MinIO .NET SDK documentation](https://min.io/docs/minio/linux/developers/dotnet/API.html)
- [MinIO server-side encryption](https://min.io/docs/minio/linux/administration/server-side-encryption.html)
- [MinIO IAM policy documentation](https://min.io/docs/minio/linux/administration/identity-access-management/policies.html)
