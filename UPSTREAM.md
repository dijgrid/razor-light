# Upstream history

This project is an independently maintained continuation of
[toddams/RazorLight](https://github.com/toddams/RazorLight). The repository was detached from its
GitHub fork network so it can evolve independently while preserving the original Git history and
Apache 2.0 license.

The independent line began from upstream commit
[`ad9c9bb76d000be4a820a57e8fa9942e30cc9325`](https://github.com/toddams/RazorLight/commit/ad9c9bb76d000be4a820a57e8fa9942e30cc9325),
dated July 6, 2024.

## Upstream synchronization policy

The original repository remains configured locally as the `upstream` remote. Upstream changes are
reviewed and cherry-picked intentionally rather than merged automatically:

```shell
git fetch upstream
git log --oneline --left-right master...upstream/master
```

Existing links to upstream issues and discussions are retained where they provide historical
context. New development, issue tracking, releases, and security maintenance belong to this
repository.

The existing NuGet package identity is retained for compatibility during the initial maintenance
work. Package ownership, naming, and release policy should be decided before publishing a new
release.
