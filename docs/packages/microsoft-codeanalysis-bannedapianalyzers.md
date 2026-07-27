# Microsoft.CodeAnalysis.BannedApiAnalyzers

## Catalog entry

`Microsoft.CodeAnalysis.BannedApiAnalyzers` **5.6.0** — universal catalog analyzer supplied through a shared `GlobalPackageReference` with private analyzer assets.

## Decision and scope

Use the analyzer to enforce the repository's explicit forbidden-symbol policy. The authoritative list is the root `BannedSymbols.txt`; no custom analyzer is needed for these rules.

## Recommended registration and use

`Directory.Build.props` includes `BannedSymbols.txt` as `AdditionalFiles` for every project, and the global package reference supplies the analyzer. Maintain one documentation-comment-ID rule per line with an optional semicolon-separated rationale. Current policy bans non-generic dictionaries, ambient clocks, and runtime reflection; update that one file rather than project files.

## Enterprise implementation guidance

Change a ban only with a replacement and migration plan. Keep messages actionable and explain the approved alternative. For approved narrow exceptions, prefer a documented source-level suppression that is reviewed with the affected code; do not fork the policy file for one project without governance.

## Integration with the catalog

The analyzer is globally installed alongside the other analyzer packages. `BannedSymbols.txt` is an `AdditionalFiles` integration, while `ModularBase.globalconfig` sets global analyzer behavior and `.editorconfig` handles file/path-scoped severity and style. The dynamic-keyword policy is separate MSBuild enforcement in `Directory.Build.targets`.

## Security, performance, AOT, trimming, and operations

The current bans support deterministic time handling and reduce reflection-related AOT/trimming risk, but they are not a complete security or AOT proof. Compilation-time analyzer cost does not affect runtime. Treat policy messages and suppression justifications as operational audit evidence.

## Avoid

Do not remove a ban to unblock an implementation, use noncanonical type names instead of documentation IDs, add a custom analyzer for the listed symbols, or assume this policy makes untrusted reflection safe.

## Verification checklist

- Add a temporary future-project use of a banned symbol and confirm the diagnostic identifies the configured rationale.
- Verify `BannedSymbols.txt` is included as `AdditionalFiles` through `Directory.Build.props`.
- Confirm approved replacements compile under the global analyzer and configuration rules.
- Review every policy-file edit with its migration impact.

## Sources

- https://www.nuget.org/packages/Microsoft.CodeAnalysis.BannedApiAnalyzers/5.6.0 (Accessed 2026-07-27)
- https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.BannedApiAnalyzers/BannedApiAnalyzers.Help.md (Accessed 2026-07-27)
- https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files (Accessed 2026-07-27)
