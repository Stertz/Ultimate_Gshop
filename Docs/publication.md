# Public Repository Setup

Destination: [Stertz/Ultimate_Gshop](https://github.com/Stertz/Ultimate_Gshop), created by the user with `main` as its default branch. The original repository remains the local `upstream`; `origin` targets the new destination. Existing commits, authorship, tags, license, and João's explicit README credit are retained.

The GitHub connector confirmed administrator/push access and an empty public destination. The local Git transport cannot connect to `github.com:443` inside this executor. The reviewed local commits can be pushed from a normal PowerShell terminal with network access:

```powershell
cd C:\Users\LucasStertz\Documents\Ultimate_Gshop
git push -u origin main
```

Push existing historical tags separately when wanted; no force push or history rewrite is needed. CI should start on the first `main` push. The prerelease workflow stays disabled until publication readiness is complete and `ENABLE_PRERELEASES` is configured.

## About settings

Prepared description: `Preview desktop editor for Perfect World Gshop files, built with .NET and Avalonia for Windows, macOS, and Linux.`

Prepared topics: `perfect-world`, `gshop`, `editor`, `avalonia`, `dotnet`, `csharp`, `cross-platform`, `open-source`.

Homepage: `https://github.com/Stertz/Ultimate_Gshop/releases`.

After authenticating GitHub CLI in a terminal with network access, apply these prepared settings:

```powershell
gh repo edit Stertz/Ultimate_Gshop --description "Preview desktop editor for Perfect World Gshop files, built with .NET and Avalonia for Windows, macOS, and Linux." --homepage "https://github.com/Stertz/Ultimate_Gshop/releases" --add-topic perfect-world --add-topic gshop --add-topic editor --add-topic avalonia --add-topic dotnet --add-topic csharp --add-topic cross-platform --add-topic open-source
```

These About settings have not been applied remotely. Enable private vulnerability reporting and appropriate branch checks after validating actual CI runs. Do not advertise packaged platform support or coverage badges as successfully hosted until the corresponding checks execute.

## Review performed

The prepublication pattern scan inspected 363 historical text blobs across 90 existing commits and the current text publication set. It found no recognizable token/private-key patterns or sensitive-looking credential/key filenames. This is a limited automated scan, not a comprehensive secret/security or asset-license clearance. Game assets remain byte-identical; [third-party notices](../THIRD_PARTY_NOTICES.md) record the remaining provenance work before binary release activation.
