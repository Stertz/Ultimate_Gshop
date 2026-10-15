# Preview Releases

The release workflow is in the public repository. Its package jobs have not run on GitHub; automatic runs are skipped while activation is disabled. No downloadable release has been published by this modernization work.

Source is published at [Stertz/Ultimate_Gshop](https://github.com/Stertz/Ultimate_Gshop). See [repository settings](publication.md) for About text, branch checks, and activation guidance.

## Version source

`Ultimate_Gshop.csproj` defines `VersionPrefix` as `1.1.0`. Local builds use `alpha.local`; release builds override the suffix with `alpha.<successful-CI-run-number>`. For example, run 42 produces `1.1.0-alpha.42` and tag `v1.1.0-alpha.42`. The same value is used for assembly metadata, package names, embedded build information, and release title/tag. Existing tags through `v.1.0.2` are not reset or removed.

`scripts/Get-ReleaseVersion.ps1` reads the project's numeric prefix. When preparing a different release series, change that one prefix and review compatibility/release notes. Rerunning a CI run retains its run number and prerelease identity.

## Workflow and activation

`.github/workflows/prerelease.yml` supports two modes:

- Manual `workflow_dispatch` builds and verifies all package artifacts without publishing a GitHub Release. Use it to validate the pipeline before activation.
- Automatic publication follows successful `Build and compatibility checks` runs originating from a push to the same repository's default branch. It requires a public repository and the repository variable `ENABLE_PRERELEASES=true`.

Before enabling automatic publication, finish the planned public-history/asset-provenance review, complete [third-party notices](../THIRD_PARTY_NOTICES.md), and verify the manual workflow on the intended new repository. Set the default branch to `main` or `master`, matching the current CI push filters, or update those filters explicitly. No self-hosted runner, deployment server, or separate publishing token is required by the workflow.

Every build checks out the exact successful CI commit rather than the later state of the branch. Package jobs have read-only repository permissions, rerun the compatibility suite on their native runners, and publish self-contained output with the matching suffix. Only the final publication job receives release write permission. Pull-request CI runs and manual runs cannot enter that publication job.

The workflow uses [`workflow_run`](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows#workflow_run) with source/event/branch guards. It does not rely on a token-created tag starting another workflow.

## Package contents and checks

| Runtime | Hosted runner | Download |
| --- | --- | --- |
| Windows x64 | `windows-2025` | ZIP containing the complete application folder |
| Linux x64 | `ubuntu-24.04` | tar.gz containing the complete application folder |
| macOS Intel | `macos-15-intel` | tar.gz containing `Ultimate Gshop.app` |
| macOS Apple Silicon | `macos-15` | tar.gz containing `Ultimate Gshop.app` |

Runner architecture labels follow [GitHub's runner table](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax#standard-github-hosted-runners-for-public-repositories). Each architecture is executed on its matching runner; these are planned checks, not currently verified platform support.

`scripts/New-ReleasePackage.ps1` rejects framework-dependent output, mismatched assembly versions, and mismatched host OS/architecture. It verifies original game asset hashes, includes the source license/README/notices and `build-info.json`, preserves the complete publish layout, and runs `--verify-installation` from a working directory outside the installation folder.

The executable diagnostic initializes real application resources and the Skia raster renderer without native windows, decodes a bundled surface and embedded fallback, and performs a synthetic new-shop save/load and client/server export in an isolated temporary directory. It does not read or modify the user's saved session. The local Windows framework-dependent executable passed this check; self-contained runtime packs and the other OS builds have not been executed locally.

For macOS the complete publish payload stays together under `Contents/MacOS`, so `AppContext.BaseDirectory` still resolves the unchanged `Resources/surfaces` tree. The script writes/lints `Info.plist`; no source assets are renamed or converted. See [Avalonia macOS deployment guidance](https://docs.avaloniaui.net/docs/deployment/macos). These previews have no distribution certificate or notarization; diagnostic execution does not validate Finder/Gatekeeper installation behavior.

Each archive has a SHA-256 sidecar. The publication job requires all four archives and matching checksums, creates a draft for the exact source commit, uploads all assets, then publishes it as a prerelease. A preexisting tag or release pointing to a different source is rejected. A failed upload leaves a draft that can be retried; an already published matching release is retained. See [GitHub CLI release creation](https://cli.github.com/manual/gh_release_create).

## Remaining release validation

Native desktop startup, user interaction, independently verified game fixtures, self-contained archive extraction/download checks, confirmed redistribution notices, signing/notarization, and the actual hosted workflow remain pending. Keep prerelease status until those checks justify a stable release. Manual workflow artifacts expire after seven days; actual release assets are separate from those temporary artifacts.
