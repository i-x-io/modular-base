# Pull request

## Summary

Explain the user-visible or engineering outcome and why this change is needed.

## Linked issue

Closes #

## Validation

List the exact local checks run and any relevant environment details.

## Checklist

- [ ] The branch follows `<type>/<issue>-<description>`.
- [ ] The pull request title is a Conventional Commit and is suitable as the squash commit.
- [ ] Public API changes update `PublicAPI.Unshipped.txt` and include compatibility reasoning.
- [ ] Dependency changes include license, advisory, lock-file, and documentation review.
- [ ] `dotnet nuke Validate --configuration Release` passes locally.
- [ ] Documentation and release notes are updated where behavior changed.
- [ ] No secrets, generated build output, or unrelated changes are included.
