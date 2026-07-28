# Security policy

## Supported versions

Before the first stable release, security fixes are made on `main` and shipped
in the next available prerelease. After `1.0.0`, only the latest released minor
line is supported unless a release note explicitly says otherwise.

| Version | Supported |
| --- | --- |
| `main` / latest release | Yes |
| Older prereleases or minor lines | No, unless explicitly announced |

## Report a vulnerability

Use GitHub's private vulnerability reporting for this repository:

<https://github.com/i-x-io/modular-base/security/advisories/new>

Do not open a public issue, discussion, or pull request containing exploit
details, credentials, or an unpatched vulnerability. Include the affected
version or commit, impact, reproduction steps, and any suggested mitigation.
Maintainers will coordinate validation, a fix, an advisory, and disclosure in
the private advisory. Response and release timing depends on severity and the
complexity of a safe fix; no fixed SLA is promised.

If a credential is exposed, revoke or rotate it immediately. Removing it from
the latest commit is insufficient because Git history and downloaded workflow
logs may retain it.

## Repository security controls

The repository scans staged changes and full CI history with Gitleaks, audits
direct and transitive NuGet dependencies, reviews dependency changes in pull
requests, pins GitHub Actions by full commit SHA, lints workflows with
actionlint and zizmor, and generates a CycloneDX SBOM. These controls reduce
risk but do not replace review or private disclosure.
