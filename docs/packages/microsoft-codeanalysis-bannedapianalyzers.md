# Microsoft.CodeAnalysis.BannedApiAnalyzers

## Catalog entry

`Microsoft.CodeAnalysis.BannedApiAnalyzers` **5.6.0** — universal catalog analyzer supplied through a shared `GlobalPackageReference` with private analyzer assets.

- **Adoption:** Global analyzer
- **Owner:** IX
- **Last reviewed:** 2026-07-27
- **Review trigger:** Review when the analyzer pin, `BannedSymbols.txt` grammar/content, generated-code policy, or approved replacement APIs change.

## Decision and scope

Use the analyzer to enforce the repository's explicit forbidden-symbol policy. The authoritative list is the root `BannedSymbols.txt`; no custom analyzer is needed for these rules. Violations are reported as RS0030, while duplicate list entries are reported as RS0031.

## Recommended registration and use

The repository registers the analyzer globally in `Directory.Packages.props` and supplies the policy from `Directory.Build.props`:

```xml
<GlobalPackageReference Include="Microsoft.CodeAnalysis.BannedApiAnalyzers" Version="5.6.0"
                        PrivateAssets="all"
                        IncludeAssets="runtime; build; native; contentfiles; analyzers; buildtransitive" />
```

```xml
<ItemGroup>
  <AdditionalFiles Include="$(MSBuildThisFileDirectory)BannedSymbols.txt" />
</ItemGroup>
```

Do not duplicate that reference in projects. If a separate centrally managed repository needs a project-scoped reference, omit `Version` and keep it private:

```xml
<PackageReference Include="Microsoft.CodeAnalysis.BannedApiAnalyzers"
                  PrivateAssets="all"
                  IncludeAssets="runtime; build; native; contentfiles; analyzers; buildtransitive" />
```

Each policy entry is a documentation-comment ID followed by an optional semicolon-delimited actionable message. `//` starts a comment:

```text
// Use an injected clock so tests and production behavior are deterministic.
P:System.DateTimeOffset.UtcNow;Inject IClock and request UTC time.

// Prefer source generation or explicit statically typed registration.
N:System.Reflection;Runtime reflection is prohibited in repository source.
```

The upstream analyzer also recognizes `BannedSymbols.*.txt`, but this repository intentionally owns one root file. Its global config excludes generated code with `dotnet_banned_api_analyzer.exclude_generated_code = true`; repository source remains checked.

| Setting or input | Catalog guidance |
| --- | --- |
| `BannedSymbols.txt` as `AdditionalFiles` | Include the single root policy in every project evaluation. |
| `dotnet_banned_api_analyzer.exclude_generated_code` | Keep `true` for generated output; verify ordinary source remains analyzed. |
| `dotnet_diagnostic.RS0030.severity` | Govern forbidden-symbol violations centrally; narrow exceptions require rationale and an expiry condition. |
| Documentation-comment ID and message | Use an exact symbol ID plus an actionable approved replacement. |

## Enterprise implementation guidance

For a new ban, first identify the exact documentation ID and an approved replacement. Add the policy message, build the entire repository, migrate all existing uses, and land the policy with the migration so CI never has an unexplained red interval. Test the replacement's important behavior, not only compilation.

For existing codebases with debt, create a measured baseline outside this repository before promotion: inventory RS0030 locations, migrate bounded areas, then enable the ban only when the remaining scope is understood. A narrow source suppression is acceptable only for a reviewed exception with owner, rationale, and removal condition. This repository's warnings-as-errors policy makes enabled RS0030 warnings fail CI.

### Upgrade and rollback

Upgrade separately from policy-list edits so new analyzer behavior is distinguishable from new bans. Build the entire repository and test representative namespace, type, member, duplicate-entry, and generated-code cases; compare RS0030/RS0031 messages and locations. If parsing, host compatibility, or diagnostic behavior regresses, restore the `5.6.0` pin and lock-file entries without weakening `BannedSymbols.txt`. Roll back a policy entry only through its normal governance decision, never as a side effect of package rollback.

## Integration with the catalog

The package is globally installed alongside the other analyzer packages. `BannedSymbols.txt` is passed through `AdditionalFiles`; it is data consumed by the analyzer, not a source file governed by `.editorconfig` path sections. `ModularBase.globalconfig` owns repository-wide analyzer options, while `.editorconfig` can scope RS0030 severity to matching source. The separate dynamic-keyword and source-reflection checks in `Directory.Build.targets` are complementary MSBuild enforcement, not part of this NuGet package. Review the analyzer's [supply-chain record](../package-guidance/supply-chain.md#microsoft-codeanalysis-bannedapianalyzers) before upgrading it.

## Security, performance, AOT, trimming, and operations

The current bans support deterministic time handling and reduce unreviewed reflection that can be fragile under trimming or NativeAOT, but a clean RS0030 build is neither a security assessment nor an AOT proof. Validate replacements with the relevant security tests and `dotnet publish` mode.

The analyzer runs during compilation and has no runtime cost. A very large or duplicated policy can increase analysis and triage work; RS0031 helps detect duplicates. Treat `BannedSymbols.txt`, replacement messages, and suppression justifications as reviewed operational policy. Restore the analyzer only from approved feeds because compiler-loaded analyzers execute during the build.

## Avoid

Do not remove a ban merely to unblock a build, use a display name when a documentation ID is required, fork the root policy for one project without governance, omit an actionable replacement, ban an API before migrating existing uses, or assume the policy makes reflection or input handling safe.

## Verification checklist

- [ ] Confirm version `5.6.0` is supplied only by the central global reference with `PrivateAssets=all`.
- [ ] Confirm root `BannedSymbols.txt` is present in each project evaluation as `AdditionalFiles`.
- [ ] Add a temporary use of one banned symbol and verify RS0030 includes the configured replacement message locally and in CI.
- [ ] Confirm generated output remains excluded while ordinary repository source is analyzed.
- [ ] Build and test every approved replacement before adding or tightening a ban.
- [ ] Review each policy or suppression edit with its migration and expiry impact.

## Sources

- [Microsoft.CodeAnalysis.BannedApiAnalyzers 5.6.0 on NuGet](https://www.nuget.org/packages/Microsoft.CodeAnalysis.BannedApiAnalyzers/5.6.0) (Accessed 2026-07-27)
- [Roslyn: Banned API analyzer configuration and documentation-ID examples](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.BannedApiAnalyzers/BannedApiAnalyzers.Help.md) (Accessed 2026-07-27)
- [Microsoft: XML documentation ID strings for symbols](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/#id-strings) (Accessed 2026-07-27)
- [Microsoft: Configuration files for .NET code-analysis rules](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files) (Accessed 2026-07-27)
