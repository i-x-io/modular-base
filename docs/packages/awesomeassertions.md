# AwesomeAssertions

## Catalog entry

`AwesomeAssertions` **9.5.0** — test-only catalog package; expressive assertions with detailed failure diagnostics.

## Decision and scope

Use in future test projects to make test intent and failed expectations easy to inspect. It complements xUnit assertions; it does not replace production validation or create test fixtures.

## Recommended registration and use

Reference it only from projects with `IXModularityProjectRole=Test` or `ArchitectureTest`; those roles also require `<IsTestProject>true</IsTestProject>`. Import `AwesomeAssertions` in tests, use direct value assertions for simple outcomes, and use `BeEquivalentTo` only with an explicit configuration when members, ordering, or exclusions matter.

## Enterprise implementation guidance

Prefer assertions against observable contracts over deep object graphs. Keep the equivalency policy local to the test or suite, make time/culture/ordering explicit, and include safe diagnostic context without leaking secrets.

## Integration with the catalog

Use with `xunit-v3.md` for test execution and `testcontainers-postgresql.md` or `testcontainers-redis.md` for integration assertions. The central catalog and test-only enforcement are defined in `Directory.Packages.props` and `Directory.Build.targets`.

## Security, performance, AOT, trimming, and operations

Assertion formatting can enumerate or serialize large graphs and include subject values in failures. Do not assert raw credentials, tokens, or personal data. It belongs only in test assemblies; no production AOT/trimming requirement follows from this package.

## Avoid

Do not use unconstrained deep equivalency as a replacement for testing a public contract, rely on incidental member ordering, or add it to a production project.

## Verification checklist

- Add a focused assertion test for the intended comparison semantics.
- Verify a deliberately failing test has useful, non-sensitive output.
- Run the test project through the xUnit v3/VSTest path before relying on the assertion extension imports.

## Sources

- https://www.nuget.org/packages/AwesomeAssertions/9.5.0 (Accessed 2026-07-27)
- https://github.com/AwesomeAssertions/AwesomeAssertions (Accessed 2026-07-27; Context7 consulted first)
