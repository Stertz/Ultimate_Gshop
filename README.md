# ✨ Ultimate Gshop
**🛒 Gshop Editor Tool for the Perfect World Game**  

[![.NET 10.0](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Avalonia UI](https://img.shields.io/badge/UI-Avalonia%2012.1.4-FF6600?logo=avalonia&logoColor=white)](https://avaloniaui.net/)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE)  
![Status](https://img.shields.io/badge/Status-Preview-orange.svg)

Ultimate Gshop is a modern and lightweight editor for **Gshop** files used in *Perfect World*.  
Originally designed for **Gshop >= v1.7.4**; exact supported variants require game-format fixture validation during modernization.

---

## 🚀 Main Features
- 🖊️ **Edit categories and items** with ease.  
- 🎨 **Modern UI** built with Avalonia & Fluent theme.  
- 🔍 Integrated **search and filtering**.  
- ⚡ Instant save/export to `.gshop` format.  
- 🖥️ Windows, macOS, and Linux are intended targets; packaged execution still needs validation.

---

## 📸 Screenshots

| Main Window | Menu | Sales Editor |
|-------------|------|--------------|
| <img src="Docs/main.png" alt="Main Window"/> | <img src="Docs/menu.png" alt="Menu"/> | <img src="Docs/edit_sales.png" alt="Sales Editor"/> |

---

## 🛠️ Built With
- **.NET 10.0** (SDK selected by `global.json`)
- **Avalonia UI 12.1.4**, with independently versioned DataGrid, ItemsRepeater, and ReactiveUI integration packages

## Build and compatibility tests

This checkout is being modernized. The updated application builds on Windows with warnings treated as errors. Native desktop startup and game compatibility still require validation; framework targeting alone does not establish support for an operating system.

```sh
dotnet restore Ultimate_Gshop.csproj
dotnet build Ultimate_Gshop.csproj --configuration Release --no-restore
dotnet run --project Ultimate_Gshop.csproj

dotnet restore tests/Ultimate_Gshop.Format.Tests/Ultimate_Gshop.Format.Tests.csproj
dotnet test --project tests/Ultimate_Gshop.Format.Tests/Ultimate_Gshop.Format.Tests.csproj --configuration Release --no-restore
```

The 149 passing compatibility tests reference the compiled application and cover synthetic client/server serialization, recoverable session saving, item editing, persisted undo/redo, and real Skia image decoding. Whole-application coverage is currently 18.41% of executable lines and 20.70% of branches; UI and independent game-file validation remain pending. All 4,580 source resources and 4,554 published game surfaces match their original paths and SHA-256 hashes. Assets retain their original names, formats, and contents.

See [testing instructions](Docs/testing.md), [migration status](Docs/stack-update.md), and the [modernization plan](Docs/modernization-plan.md).

Session saves/imports now use a [recoverable multi-file protocol](Docs/persistence.md), preserving user preferences while resetting history for a new shop. The [prerelease pipeline](Docs/releasing.md) is prepared for self-contained Windows/Linux/macOS downloads, but hosted execution and public publication are still pending.

Public project: [Stertz/Ultimate_Gshop](https://github.com/Stertz/Ultimate_Gshop). Downloadable previews will appear under [Releases](https://github.com/Stertz/Ultimate_Gshop/releases) after the pipeline is validated and activated.

## Contributing

Read [CONTRIBUTING.md](CONTRIBUTING.md) for development commands, compatibility requirements, and pull request guidance. Community participation follows the [code of conduct](CODE_OF_CONDUCT.md); security concerns follow the [security reporting policy](SECURITY.md).

---

## 📄 License
Licensed under the **GNU General Public License v3.0**.  
See the [LICENSE](LICENSE) file for details.  

---

💡 *Created by* **[João Carlos Ilha Baierle](https://github.com/joaocib)** & **[Lucas Fernando Padilha Stertz](https://github.com/Stertz)**.

João collaborated on the original implementation and helped build this project. The original Git history is preserved to credit that work.
