# SonarAnalyzer.CSharp

## Catalog entry

`SonarAnalyzer.CSharp` **10.30.0.144632** — universal catalog analyzer supplied through a shared `GlobalPackageReference` with private analyzer assets.

- **Owner:** IX
- **Last reviewed:** 2026-07-27
- **Review trigger:** Review when the analyzer pin, `Sxxxx` rule metadata/defaults, Sonar quality profile/connected mode, or repository security policy changes.

## Decision and scope

Use as the repository's compile-time Sonar rule set for future C# code. It supplies `Sxxxx` diagnostics for bugs, vulnerabilities, security hotspots, and code smells. This NuGet package is not SonarQube Server, SonarQube Cloud, SonarQube for IDE, or SonarScanner analysis; it does not upload source or diagnostics by itself and does not replace code review, dependency scanning, penetration testing, or threat modeling.

## Recommended registration and use

The repository already owns the global registration in `Directory.Packages.props`:

```xml
<GlobalPackageReference Include="SonarAnalyzer.CSharp" Version="10.30.0.144632"
                        PrivateAssets="all"
                        IncludeAssets="runtime; build; native; contentfiles; analyzers; buildtransitive" />
```

Do not duplicate it in projects. In a separate centrally managed repository that requires project scope, omit `Version` and retain private assets:

```xml
<PackageReference Include="SonarAnalyzer.CSharp"
                  PrivateAssets="all"
                  IncludeAssets="runtime; build; native; contentfiles; analyzers; buildtransitive" />
```

Configure standalone NuGet analysis with normal Roslyn severity entries. For example, S2245 flags pseudorandom values used in security-sensitive contexts:

```editorconfig
[*.cs]
dotnet_diagnostic.S2245.severity = error

[test/**/*.cs]
dotnet_diagnostic.S2245.severity = warning
```

Use `ModularBase.globalconfig` for repository defaults and `.editorconfig` only for justified file/path-specific policy. If the organization later introduces Sonar connected mode or scanner analysis, reconcile its quality profile and exclusions with this build-time policy deliberately; those products have separate configuration and data-flow considerations.

| Setting or boundary | Catalog guidance |
| --- | --- |
| `dotnet_diagnostic.Sxxxx.severity` | Use for standalone NuGet analyzer policy and narrow reviewed source scopes. |
| `PrivateAssets="all"` | Keep the analyzer build-only and out of package-consumer graphs. |
| Sonar quality profile | Treat as a separate server/connected-mode source of policy; reconcile it explicitly with committed AnalyzerConfig. |
| Analysis exclusions | Scope to generated/vendor code with an owner and rationale; never broadly exclude security-sensitive source. |

## Enterprise implementation guidance

Start with a full representative build and inventory new `Sxxxx` diagnostics by defect class, security relevance, and overlap with other analyzers. Fix high-confidence bugs and vulnerabilities first. For existing debt, baseline only specific diagnostic IDs and paths with an owner and removal condition; do not turn off the complete Sonar rule family.

Review security hotspots with system context: determine whether input is attacker-controlled, identify the trust boundary, verify the proposed mitigation, and add a regression test. A false-positive decision needs a durable rationale. CI should run the same committed analyzer configuration as developer builds and retain normal build diagnostics as evidence. Treat changes to the NuGet analyzer version, Sonar quality profile, or connected-mode binding as separate policy changes that require diagnostic-diff review.

### Upgrade and rollback

Upgrade the NuGet analyzer separately from quality-profile or connected-mode changes. Build representative projects and diff rule keys, categories, default severities, messages, hotspot behavior, code fixes, and clean-build cost; threat-model new security findings before suppression. If the analyzer cannot run on a supported compiler host, produces unacceptable policy drift, or materially regresses builds, restore `10.30.0.144632` and lock files. Keep validated security fixes, but remove suppressions created only for the failed version; server profile rollback, if applicable, is a distinct governed action.

## Integration with the catalog

This package shares the global analyzer mechanism with `meziantou-analyzer.md`, `roslynator-analyzers.md`, and the other universal analyzers. Repository-wide defaults are in `ModularBase.globalconfig`; matching `.editorconfig` entries take precedence. Central package management owns exact version `10.30.0.144632`, and `PrivateAssets=all` prevents analyzer assets from flowing to package consumers. Scanner or connected-mode tooling is not installed by this catalog entry. Review its [supply-chain record](../package-guidance/supply-chain.md#sonaranalyzer-csharp) before changing analyzer provenance or connected tooling.

## Security, performance, AOT, trimming, and operations

Sonar diagnostics can identify security defects and review hotspots, but a clean build is not security approval and cannot establish exploitability or absence of vulnerabilities. Never place credentials or sensitive source excerpts in public suppression discussions or unprotected artifacts. Before adding connected services, review authentication, source upload, retention, and access controls separately.

The NuGet analyzer runs in compiler and IDE processes, increasing build CPU and memory but adding no application runtime dependency. On large or generated files, capture clean-build timing and diagnostic evidence before applying a narrow rule/path exclusion; do not broadly disable analysis. The analyzer itself has no trimming or NativeAOT effect, although fixes can change runtime behavior and still need publish testing. Restore only from approved feeds.

## Avoid

Do not equate a clean analyzer build with security approval, suppress a security issue without threat-model evidence, duplicate or version the package in project files, assume this package uploads results or enforces a server quality gate, install scanner/connected-mode tooling implicitly, or publish diagnostics containing sensitive implementation details.

## Verification checklist

- [ ] Confirm central version `10.30.0.144632`, `PrivateAssets=all`, and no project-local duplicates.
- [ ] Build a representative C# project and record expected `Sxxxx` diagnostics locally and in CI.
- [ ] Verify global and scoped `.editorconfig` precedence for one rule.
- [ ] Review security findings through the owning threat-model and security process, with regression tests for accepted fixes.
- [ ] Compare diagnostic counts and clean-build performance before version or quality-policy changes.
- [ ] Inspect a packed library and confirm Sonar analyzer assets do not flow to consumers.

## Sources

- [SonarAnalyzer.CSharp 10.30.0.144632 on NuGet](https://www.nuget.org/packages/SonarAnalyzer.CSharp/10.30.0.144632) (Accessed 2026-07-27)
- [SonarSource sonar-dotnet analyzer repository](https://github.com/SonarSource/sonar-dotnet) (Accessed 2026-07-27)
- [SonarQube for Visual Studio: C# and VB.NET rule configuration](https://docs.sonarsource.com/visual-studio/using/rules) (Accessed 2026-07-27)
- [SonarQube for Visual Studio: connected mode behavior](https://docs.sonarsource.com/sonarqube-for-visual-studio/team-features/connected-mode) (Accessed 2026-07-27)
- [Microsoft: Configuration files for .NET code-analysis rules](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files) (Accessed 2026-07-27)
