# AwesomeAssertions

## Catalog entry

| Field | Value |
| --- | --- |
| Package | `AwesomeAssertions` |
| Pinned version | `9.5.0` |
| Status | Approved test-only dependency |
| Role | Expressive assertions with detailed failure diagnostics |
| Owner | IX |
| Last reviewed | 2026-07-27 |
| Review trigger | `AwesomeAssertions` version, target-framework, assertion-extension API, or supported test-framework change |

## Decision and scope

Use in test and architecture-test projects to make expected behavior and failures readable. It complements xUnit's execution model; it does not create fixtures, replace production validation, or justify assertions against implementation details.

## Recommended registration and use

Reference the centrally pinned package without a project-local version, and only from a project whose role is `Test` or `ArchitectureTest` and which sets `IsTestProject=true`:

```xml
<PackageReference Include="xunit.v3" />
<PackageReference Include="AwesomeAssertions" />
```

Import `AwesomeAssertions` and assert the smallest observable contract. Configure graph equivalency explicitly when ordering, exclusions, or member mappings matter:

```csharp
using AwesomeAssertions;

[Fact]
public async Task LoadAsync_returns_the_expected_customer()
{
    var actual = await LoadAsync();

    actual.Should().BeEquivalentTo(
        new Customer(42, "Ada", DateTimeOffset.UnixEpoch),
        options => options.WithStrictOrdering());
}
```

For failure paths, capture the action and await the assertion so the test observes the asynchronous exception:

```csharp
Func<Task> act = () => service.LoadAsync(-1);

await act.Should().ThrowExactlyAsync<ArgumentOutOfRangeException>();
```

## Enterprise implementation guidance

Use direct assertions for scalar outcomes and `BeEquivalentTo` for DTO/value-object contracts whose comparison policy is visible in the test. Use `AssertionScope` when several related facts should be reported together, not to combine unrelated behaviors into one test. Keep clocks, cultures, ordering, and generated identifiers deterministic; add a `because` message only when it explains business intent.

When a snapshot-sized graph fails, narrow the subject or exclusions instead of enabling broad global equivalency rules. Run an intentionally failing example locally to review the diagnostic payload before enabling it in CI.

### Upgrade and rollback

Upgrade the central pin in a focused change, then compile every custom assertion extension and run representative scalar, exception, and object-graph assertions, including one intentional failure to inspect diagnostics. Major-version migrations may rename namespaces or change the custom-assertion API; follow the upstream migration guide rather than applying a broad textual replacement without compilation. Roll back by restoring the previous central pin and any compatible namespace or extension changes together. Assertions do not migrate persistent data, so rollback is code-and-package only.

## Integration with the catalog

Use with [xunit.v3](xunit-v3.md) for the preferred MTP test stack and with the Testcontainers guides for integration assertions. The central version and test-role restriction are owned by `Directory.Packages.props` and `Directory.Build.targets`; project references stay versionless. See [test-platform, runner, and coverage selection](../package-guidance/package-selection.md#test-platform-runners-and-coverage) and the [AwesomeAssertions supply-chain entry](../package-guidance/supply-chain.md#awesomeassertions).

## Security, performance, AOT, trimming, and operations

Failure formatting can traverse large graphs and print subject values. Never assert raw credentials, tokens, connection strings, personal data, or production payloads; redact or project them to safe values first. Bound large collection assertions to avoid expensive diagnostics. This package belongs only in test assemblies and creates no production trimming or NativeAOT requirement.

## Avoid

- Do not use unconstrained deep equivalency as a substitute for a public contract.
- Do not rely on incidental collection/member ordering.
- Do not put shared mutable assertion configuration in parallel tests.
- Do not reference the package from a production project.

## Verification checklist

- [ ] The project reference is versionless and limited to a `Test` or `ArchitectureTest` project.
- [ ] Comparison rules make ordering, time, culture, and exclusions explicit where relevant.
- [ ] A deliberately failing test produces useful diagnostics without sensitive values.
- [ ] The test runs through the project's selected xUnit v3 runner stack.

## Sources

- [Awesome Assertions introduction and assertion scopes](https://awesomeassertions.org/introduction)
- [Awesome Assertions object-graph equivalency](https://awesomeassertions.org/objectgraphs/)
- [Awesome Assertions exception assertions](https://awesomeassertions.org/exceptions/)
- [Awesome Assertions version 9 migration guide](https://awesomeassertions.org/upgradingtov9)
- [AwesomeAssertions 9.5.0 on NuGet](https://www.nuget.org/packages/AwesomeAssertions/9.5.0)

Accessed 2026-07-27. Context7 was consulted first.
