# FluentStorage.GCP

## Catalog entry

| Field | Value |
| --- | --- |
| Package | `FluentStorage.GCP` |
| Pinned version | `8.0.14` |
| Role | Google Cloud Storage implementation of FluentStorage `IStore` |
| Status | Approved for GCS object workloads; native APIs remain required for GCS-specific preconditions and lifecycle controls |

## Decision and scope

Use this package for a selected Google Cloud Storage bucket. Object names are opaque keys and `/` supplies a prefix convention, not directory semantics. The provider rejects `append: true`; GCS does not support general object append through this shared API.

## Recommended registration and use

Reference the provider without a version; the central catalog supplies `8.0.14`:

```xml
<ItemGroup>
  <PackageReference Include="FluentStorage.GCP" />
</ItemGroup>
```

Use Application Default Credentials (ADC) and the environment-based factory for hosted and local development:

```csharp
using FluentStorage;
using FluentStorage.Storage;

IStore store = GoogleCloudStorage.FromEnvironmentVariable(
    bucketName: "modular-base-documents");
```

ADC is resolved by the Google client libraries from their documented credential locations. In production use an attached service account or workload identity federation with least privilege. `FromJsonFile` and `FromJson` exist for controlled compatibility cases, but service-account key material must not be committed, copied into standard configuration, or logged. If your platform must configure retry, transport, encryption key behavior, or preconditions directly, use the native GCS client in an application-owned adapter.

For local development, establish ADC with the Google Cloud CLI under the developer identity rather than placing a service-account JSON key in the repository. In deployment, bind a service account/workload identity and grant only required bucket/object permissions. Provision the bucket, region, uniform bucket-level access, retention, and lifecycle separately. After construction, use the shared upload/download/list/delete workflow in [FluentStorage](fluentstorage.md); keep listings prefix-bounded and never interpret a prefix delete as atomic. Use native generation preconditions for create-only, compare-and-swap, and generation-specific deletes.

Stream large content and dispose all returned streams. Do not use `GetBytes`/`GetText` on unbounded objects. FluentStorage normalizes separator characters but does not sanitize semantic path segments.

## Enterprise implementation guidance

- Prefer ADC and short-lived federated/workload credentials. Grant the workload only the GCS IAM role and bucket/prefix scope it needs.
- Google Cloud client libraries retry some operations by default. Define retries once, with a bounded timeout budget, and avoid automatic outer retries for writes whose outcome is uncertain. Use idempotent object keys and GCS generation/metageneration preconditions through native APIs when concurrency matters.
- Enable and enforce encryption, retention, object versioning, lifecycle, uniform bucket-level access, VPC Service Controls/private networking, audit logging, and regional placement through bucket/org configuration. Customer-managed/encryption-key-specific behavior is not a generic `IStore` feature.
- Record sanitized bucket/prefix, operation, object generation where applicable, bytes, duration, status and request/correlation identifiers; exclude credentials, object data, and sensitive full keys.

## Integration with the catalog

- Shared path, stream, disposal, and retry guidance: [FluentStorage](fluentstorage.md).
- S3-compatible on-prem/object-storage alternative: [FluentStorage.Minio](fluentstorage-minio.md).
- Coordinate any outer resilience policy with the catalog Polly/Microsoft resilience entries after the GCS retry policy is configured.

## Security, performance, AOT, trimming, and operations

GCS listings are prefix/pagination operations, not directory reads. A `ObjectExists` then write sequence is not an atomic create. Use native conditional operations for immutable ingest or compare-and-swap behavior. Test stream seekability and large upload behavior in the deployed runtime. The package has no documented trimming or Native AOT guarantee; prove any claim with a publish-and-run test.

## Avoid

- Do not use `append: true` or treat prefixes as transactional directories.
- Do not use JSON service-account keys as the routine production credential mechanism.
- Do not layer Polly/Microsoft resilience retries blindly over native client retries.
- Do not assume generic `SetObject` provides GCS generation preconditions, retention locks, CMEK, or lifecycle policy.

## Verification checklist

- [ ] Restore resolves `FluentStorage.GCP` `8.0.14` and `FluentStorage` `8.0.16` without downgrade warnings.
- [ ] ADC works locally without committed credential files and uses the intended workload identity in the deployed environment.
- [ ] IAM access, bucket encryption/retention/lifecycle/versioning, network controls, and audit logging are least-privilege and tested.
- [ ] Tests cover overwrite, rejected append, stream disposal, cancellation, and native generation preconditions when required.
- [ ] Retry/timeout budget and unknown-write reconciliation are documented and load-tested.

## Sources

Accessed 2026-07-27.

- [FluentStorage GCP factory/source](https://github.com/robinrodricks/FluentStorage/tree/develop/FluentStorage.GCP)
- [FluentStorage.GCP 8.0.14 on NuGet](https://www.nuget.org/packages/FluentStorage.GCP/8.0.14)
- [Google Cloud Application Default Credentials](https://cloud.google.com/docs/authentication/application-default-credentials)
- [Google Cloud Storage authentication](https://cloud.google.com/storage/docs/authentication)
- [Cloud Storage retry strategy](https://cloud.google.com/storage/docs/retry-strategy)
- [Cloud Storage encryption](https://cloud.google.com/storage/docs/encryption)
