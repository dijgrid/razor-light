# Release process

RazorLight's independently maintained packages use a protected, tag-triggered release process. This
document is an operator checklist; ordinary pull requests and CI runs never publish packages.

## Package identity

| Project | NuGet package | First independent version |
| --- | --- | --- |
| RazorLight library | `Dijgrid.RazorLight` | `3.0.0` |
| Optional HTML integration | `Dijgrid.RazorLight.Html` | `3.0.0` |
| Precompile tool | `Dijgrid.RazorLight.Precompile` | `3.0.0` |

The package IDs distinguish the independent line from the historical packages. CLR namespaces and
the `razorlight-precompile` tool command remain unchanged. DECISION-003 records the identity and
versioning rationale.

## One-time publishing controls

The GitHub repository must have an environment named `nuget` with the `dijgrid` user as a required
reviewer. Restrict deployment branches and tags to `v*.*.*`.

In the `dijgrid` NuGet.org account, create a trusted-publishing policy with these exact values:

| Field | Value |
| --- | --- |
| Policy owner | `dijgrid` |
| Repository owner | `dijgrid` |
| Repository | `razor-light` |
| Workflow file | `release.yml` |
| Environment | `nuget` |

The release job requests a one-time credential through GitHub OIDC. Do not create or store a
long-lived NuGet API key for this workflow.

## Pull-request and default-branch artifacts

CI builds both `.nupkg` and `.snupkg` files, validates their manifests and contents, resolves their
Source Link documents, and uploads them as the `release-candidate-packages` workflow artifact. CI
also rebuilds the library and tool twice and compares their DLL and PDB hashes.

For a local artifact review:

```powershell
dotnet restore RazorLight.sln
dotnet tool restore
dotnet build RazorLight.sln --configuration Release --no-restore --warnaserror
pwsh ./scripts/Test-DeterministicBuild.ps1
dotnet pack src/RazorLight/RazorLight.csproj --configuration Release --no-build --output artifacts/packages
dotnet pack src/RazorLight.Html/RazorLight.Html.csproj --configuration Release --no-build --output artifacts/packages
dotnet pack src/RazorLight.Precompile/RazorLight.Precompile.csproj --configuration Release --no-build --output artifacts/packages
pwsh ./scripts/Validate-Packages.ps1 -PackageDirectory artifacts/packages -Version 3.0.0
pwsh ./scripts/Test-PackageConsumer.ps1 -PackageDirectory artifacts/packages -Version 3.0.0
```

Inspect the package files with a NuGet package viewer before approving a release. Confirm the IDs,
version, .NET 10 target, dependencies, license, README, repository commit, tool command, and symbols.

## Creating a release

1. Complete all intended changes on `master` and ensure required CI checks pass.
2. Move the `Unreleased` changelog entries into a heading for the release version and date.
3. Set `VersionPrefix` and, for a prerelease, `VersionSuffix` in `Directory.Build.props` so the
   evaluated project `Version` exactly matches the release version.
4. Run the full local validation and inspect the latest default-branch package artifact.
5. Create an annotated tag whose version exactly matches the evaluated project `Version`, then push
   only that tag:

   ```shell
   git tag -a v3.0.0 -m "RazorLight 3.0.0"
   git push origin v3.0.0
   ```

6. The release workflow rebuilds, tests, validates, and uploads a second artifact set. Download and
   review that set before approving the waiting `nuget` environment deployment.
7. Approval allows the workflow to exchange its OIDC token for a short-lived NuGet credential,
   publish all packages and symbols, and attach the same files to a matching GitHub Release.
8. Verify all NuGet package pages, symbol processing, installation, the GitHub Release assets, and
   the release notes.

The workflow accepts SemVer tags in `v<major>.<minor>.<patch>[-prerelease]` form, requires an exact
match with the evaluated project `Version`, and requires the commit to be contained in
`origin/master`. A prerelease tag creates a GitHub prerelease. Published versions are immutable;
never move or reuse a release tag.

## Failed or partial release

- Before environment approval, cancel the workflow, delete the unpublished tag if necessary, fix
  the problem on `master`, and create a new tag only after validation passes.
- After any NuGet package is published, do not overwrite or reuse that version. Finish the matching
  package only when the existing artifacts are known-good; otherwise unlist the affected version and
  prepare a patch release.
- If package contents or credentials may be compromised, unlist the package version, disable the
  NuGet trusted-publishing policy and GitHub environment, and follow `SECURITY.md`.
- A GitHub Release may be drafted or removed while repairing release notes, but do not delete or move
  a tag that identifies an already-published NuGet version.
- Record the incident, affected artifacts, recovery action, and replacement version in the changelog
  and repository planning records.

NuGet.org generally does not permit deleting a published package. Unlisting removes it from search
while preserving restores for consumers that already reference the exact version.
