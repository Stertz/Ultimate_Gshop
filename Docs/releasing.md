# Packaging

## Workflow

`.github/workflows/prerelease.yml` builds self-contained packages for `win-x64`, `linux-x64`, `osx-x64` and `osx-arm64` on matching native runners.

Manual `workflow_dispatch` validates packages and uploads workflow artifacts. Automatic publication follows a successful default-branch push CI run when `ENABLE_PRERELEASES=true` and the repository is public. The manual path does not publish a GitHub Release.

The numeric version prefix comes from `Ultimate_Gshop.csproj`. The workflow appends `alpha.<run-number>` and records the source commit, runtime and version in `build-info.json`.

## Archives

Windows packages use ZIP. Linux packages use tar.gz. macOS packages use tar.gz containing `Ultimate Gshop.app`, with the published payload in `Contents/MacOS`.

`scripts/New-ReleasePackage.ps1` checks native OS/architecture, self-contained runtime configuration, assembly version and resource hashes. It includes README, license, notices and build metadata, creates the archive and writes its SHA-256 sidecar.

`scripts/Test-ReleaseArchive.ps1` verifies the archive name/checksum, extracts to a temporary directory, checks metadata/resources and executes `--verify-installation` from outside the application folder.

The executable diagnostic initializes application resources and Skia, decodes bundled images and performs synthetic save/load plus client/server export in a temporary session.

## Local use

From the repository root, using PowerShell 7 on Windows x64:

```powershell
$version = ./scripts/Get-ReleaseVersion.ps1 -RunNumber 1
$suffix = $version.Substring($version.IndexOf('-') + 1)
$commit = git rev-parse HEAD
dotnet publish Ultimate_Gshop.csproj --configuration Release --runtime win-x64 --self-contained true --output artifacts/publish "-p:VersionSuffix=$suffix"
./scripts/New-ReleasePackage.ps1 -PublishDirectory artifacts/publish -Runtime win-x64 -Version $version -Commit $commit
```

Run packaging checks on the OS and architecture matching the chosen runtime. Packaging contributions and installation guides are welcome through pull requests.
