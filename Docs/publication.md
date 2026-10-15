# Repository Settings

Repository: [Stertz/Ultimate_Gshop](https://github.com/Stertz/Ultimate_Gshop).

## About

Description: `Desktop editor for Perfect World Gshop files, built with .NET and Avalonia.`

Homepage: `https://github.com/Stertz/Ultimate_Gshop/releases`.

Topics: `perfect-world`, `gshop`, `editor`, `avalonia`, `dotnet`, `csharp`.

Maintainers can apply these settings through the repository's About panel or authenticated GitHub CLI:

```powershell
gh repo edit Stertz/Ultimate_Gshop --description "Desktop editor for Perfect World Gshop files, built with .NET and Avalonia." --homepage "https://github.com/Stertz/Ultimate_Gshop/releases" --add-topic perfect-world --add-topic gshop --add-topic editor --add-topic avalonia --add-topic dotnet --add-topic csharp
```

These settings are prepared but have not been applied remotely. The available connector does not expose repository-settings writes, and the local CLI cannot connect to GitHub from the current executor.

## Branch checks and releases

The public `main` branch preserves the original Git history and both authors' credits. Local `origin` points to this repository; `upstream` retains the original source repository.

Require the three desktop-build and three compatibility-test checks for pull requests after verifying their names on GitHub. The README badge job runs only on default-branch pushes and should not be required for PRs. Enable private vulnerability reporting through repository settings.

The prerelease workflow is opt-in through `ENABLE_PRERELEASES=true`. Complete [third-party notices](../THIRD_PARTY_NOTICES.md) and validate the manual package workflow before activation; see [releasing](releasing.md). No deployment server is used.
