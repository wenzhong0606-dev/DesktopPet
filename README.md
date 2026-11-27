# AI Desktop Companion

**Current repository / engineering project name:** `BichonDesktopPet`

AI Desktop Companion is a Windows desktop companion platform in gradual development. The Bichon Frise is the first sample character, not the only character the future platform may support. The long-term direction includes multiple animals, human-style avatars, user-imported models, character-specific behavior, and optional AI conversation.

> Current product baseline: M0 image-based desktop pet display. The WPF image-loading implementation and `assets/characters/bichon/base/bichon-idle.png` are present on `main`. The committed PNG has been inspected from the repository binary: 420 × 540 pixels, RGBA, 132,158 bytes, SHA-256 `326d10ace28d53e8a4289b9f103c648ecb79d6b1b32e4a8f76d715793b97d210`. Windows runtime visual verification is still pending.

## Current technology

- C# / .NET 10
- WPF (Windows desktop)
- Shared Core library for settings and basic models
- Local JSON settings
- Optional audio, disabled by default and unavailable until an authorized audio asset is supplied

## Current implementation

The current WPF app includes a transparent 280 × 360 DIP window, PNG loading with a vector fallback, dragging, position persistence, click and gift reactions, snow, decorative lights, system tray controls, local settings persistence, and an optional audio integration point.

Some daily-use details remain to be completed in M1, including a dedicated settings window, an explicit tray hide action, and diagnosable error logging. See the verified inventory and acceptance checklist.

## Build (Windows)

```powershell
dotnet restore BichonDesktopPet.sln
dotnet build BichonDesktopPet.sln -c Release
dotnet test BichonDesktopPet.sln -c Release
```

## Run

```powershell
dotnet run --project src/BichonDesktopPet.App/BichonDesktopPet.App.csproj
```

## Product and development documents

- [Product definition and MVP roadmap](docs/PRODUCT_DEFINITION_AND_MVP_ROADMAP_V1.md)
- [M1 baseline and acceptance checklist](docs/M1_BASELINE_AND_ACCEPTANCE.md)
- [Project baseline](docs/PROJECT.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Visual specification](docs/VISUAL_SPECIFICATION.md)
- [Runtime asset status](docs/M1_ASSET_UPLOAD.md)

## Development rule

Every milestone must produce a buildable, runnable, testable increment. Do not begin the next milestone until the current one passes its acceptance gate. The current task is M1 only; AI, voice, 3D runtime, arbitrary model import, and a character marketplace are not part of M1.
