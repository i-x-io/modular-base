# Microsoft.CodeAnalysis.PublicApiAnalyzers

## Catalog entry

`Microsoft.CodeAnalysis.PublicApiAnalyzers` **5.6.0** — project-scoped catalog analyzer; packable projects opt in and own their public API baseline files.

- **Adoption:** Direct
- **Owner:** IX
- **Last reviewed:** 2026-07-27
- **Review trigger:** Review when the analyzer pin, public API baseline format, nullable API policy, or package-release compatibility process changes.

## Decision and scope

Use only for packable libraries where public API compatibility is a release contract. It is intentionally not a repository-wide global analyzer and must not be added to non-packable applications by default. It makes API additions, removals, signatures, and nullable annotations visible as reviewed text changes; package validation remains the complementary binary/source compatibility check against released packages.

## Recommended registration and use

`Directory.Packages.props` owns the version, while `Directory.Build.targets` conditionally adds this versionless private reference and both baselines:

```xml
<ItemGroup Condition="'$(IsPackable)' == 'true'">
  <PackageReference Include="Microsoft.CodeAnalysis.PublicApiAnalyzers"
                    PrivateAssets="all"
                    IncludeAssets="runtime; build; native; contentfiles; analyzers; buildtransitive" />
  <AdditionalFiles Include="$(MSBuildProjectDirectory)/PublicAPI.Shipped.txt"
                   Condition="Exists('$(MSBuildProjectDirectory)/PublicAPI.Shipped.txt')" />
  <AdditionalFiles Include="$(MSBuildProjectDirectory)/PublicAPI.Unshipped.txt"
                   Condition="Exists('$(MSBuildProjectDirectory)/PublicAPI.Unshipped.txt')" />
</ItemGroup>
```

Create both files beside every packable project and enable nullable API tracking at the top of each:

```text
# PublicAPI.Shipped.txt
#nullable enable
```

```text
# PublicAPI.Unshipped.txt
#nullable enable
```

On an intentional API addition, use the RS0016/RS0036 code fix to generate the exact entry in `PublicAPI.Unshipped.txt`, then review it rather than hand-authoring a guessed signature. RS0017 reports deleted API entries; a deliberate removal is recorded in the unshipped file with the analyzer's `*REMOVED*` form. RS0048 catches missing or unregistered baseline files, and the repository also fails `Pack` if either file is absent.

| Setting or input | Catalog guidance |
| --- | --- |
| `dotnet_public_api_analyzer.require_api_files` | Keep enabled for governed packable projects so missing baselines fail visibly. |
| `PublicAPI.Shipped.txt` | Immutable release history; promote reviewed entries here during the release process, never to hide a break. |
| `PublicAPI.Unshipped.txt` | Development delta for additions and explicit removals; review it with the source change. |
| `#nullable enable` | Keep at the top of both files so nullability remains part of the API contract. |
| RS diagnostic severity | Configure per diagnostic in shared AnalyzerConfig policy; do not blanket-suppress API drift. |

## Enterprise implementation guidance

Use this release workflow:

1. Add or change the public API and build the packable project.
2. Apply the analyzer code fix for intended additions, then review nullability, generic constraints, parameter defaults, and overload shape in the unshipped diff.
3. Treat removals and signature changes as compatibility decisions requiring the repository's versioning and migration process; never make RS0017 disappear by silently rewriting shipped history.
4. Before release, promote accepted unshipped additions into `PublicAPI.Shipped.txt` as part of a dedicated release change and leave the unshipped file ready for the next development cycle.
5. Run build, test, pack, and package validation in CI. Review both the source API baseline diff and compatibility output.

When adopting the analyzer for an existing package, generate a complete baseline from the current released-compatible surface, review it, and commit it as a distinct baseline change before enforcing subsequent diffs.

### Upgrade and rollback

Upgrade the analyzer in a dedicated policy change and build, test, and pack every governed project without rewriting baseline files first. Inventory new RS diagnostics and baseline-format changes, then compare package validation against the last released package. Accept generated baseline edits only after reviewing the actual public surface. If the upgrade produces incorrect entries, breaks the release workflow, or cannot run on the supported SDK, restore the `5.6.0` pin and lock-file resolution while leaving shipped history unchanged; revert upgrade-induced unshipped churn separately so intentional API work is preserved.

## Integration with the catalog

This project-scoped analyzer differs from the universal `GlobalPackageReference` analyzers. `Directory.Build.targets` activates it only when `IsPackable=true`; central package management supplies version `5.6.0`, and `PrivateAssets=all` prevents it from becoming a consumer dependency. The same targets add the two adjacent files as `AdditionalFiles` and validate them before pack. Common severity policy remains in `ModularBase.globalconfig` and `.editorconfig`. Review its [supply-chain record](../package-guidance/supply-chain.md#microsoft-codeanalysis-publicapianalyzers) before upgrading release tooling.

## Security, performance, AOT, trimming, and operations

API baselines can reveal accidental expansion of a security-sensitive surface, but they do not evaluate authorization, unsafe input, or implementation safety. Review every newly exposed type and member against the threat model. Nullability tracking improves the API contract but does not replace runtime validation.

Analysis occurs at compile time and has no runtime, trimming, or NativeAOT cost. Large public surfaces add build and review work; use scoped packable-project activation rather than repository-wide installation. Because analyzer and baseline changes can block releases, test upgrades on all packable projects and keep baseline promotion in the release checklist. Validate actual trimming/AOT behavior separately when the public contract is used by such consumers.

## Avoid

Do not add the analyzer globally, install it in non-packable projects without an explicit API contract, hand-edit generated signatures without rebuilding, suppress a mismatch merely to ship, delete or rewrite shipped entries to conceal a breaking change, or treat an unchanged baseline as proof of binary compatibility.

## Verification checklist

- [ ] Confirm every packable project owns `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` with `#nullable enable`.
- [ ] Confirm the conditional versionless `PackageReference` resolves central version `5.6.0` with `PrivateAssets=all`.
- [ ] Add a temporary public member and verify RS0016 or RS0036 produces a reviewable unshipped entry.
- [ ] Remove or change a baseline member and verify the build reports the compatibility diagnostic.
- [ ] Run `dotnet pack` and confirm missing baseline files fail the repository's pack policy.
- [ ] Confirm non-packable projects do not reference or run this analyzer unless explicitly approved.

## Sources

- [Microsoft.CodeAnalysis.PublicApiAnalyzers 5.6.0 on NuGet](https://www.nuget.org/packages/Microsoft.CodeAnalysis.PublicApiAnalyzers/5.6.0) (Accessed 2026-07-27)
- [Roslyn: Public API analyzer setup, diagnostics, and baseline format](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md) (Accessed 2026-07-27)
- [Microsoft: Package validation overview](https://learn.microsoft.com/en-us/dotnet/fundamentals/apicompat/package-validation/overview) (Accessed 2026-07-27)
- [Microsoft: Configuration files for .NET code-analysis rules](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files) (Accessed 2026-07-27)
