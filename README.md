# AI Desktop Companion

**Current repository / engineering project name:** `BichonDesktopPet`

AI Desktop Companion is a Windows desktop companion platform in gradual development. The Bichon Frise is the first sample character, not the only character the future platform may support. The long-term direction includes multiple animals, human-style avatars, user-imported models, character-specific behavior, and optional AI conversation.

> Current product baseline: M0 image-based desktop pet display. The WPF image-loading implementation and `assets/characters/bichon/base/bichon-idle.png` are present on `main`. The committed PNG is 420 × 540 pixels, RGBA, 132,158 bytes, SHA-256 `326d10ace28d53e8a4289b9f103c648ecb79d6b1b32e4a8f76d715793b97d210`.

## Current technology

- C# / .NET 10
- WPF (Windows desktop)
- Shared Core library for settings and basic models
- Local JSON settings
- Optional local background audio; disabled by default and unavailable until an authorized audio asset is supplied

## Current implementation

The current WPF app includes a transparent 280 × 360 DIP window, PNG loading with a vector fallback, dragging, position persistence, click and gift reactions, snow, decorative lights, system tray controls, local JSON settings, and an optional audio integration point.

The user confirmed that the previous M1 desktop-pet and tray hide/show baseline works normally on their Windows computer. The current M1 update adds:

- A dedicated settings window, accessible from the pet context menu and system tray.
- Immediate application and persistence of always-on-top, snow, decorative-light, and music preferences.
- A default-position reset action.
- A disabled music control with an explanation when the optional audio asset is absent.
- Local application diagnostics at `%LOCALAPPDATA%\BichonDesktopPet\logs\app.log`.
- Testable settings persistence in the Core project, with round-trip, missing-file, corrupt-file and save-failure coverage.

**Automated verification:** Windows CI passed on commit `a6dbd3ea9bda607d2d19b224d4975e9647cec205`: Release build succeeded with 0 warnings and 0 errors; all 7 tests passed. [View Windows CI run](https://github.com/wenzhong0606-dev/DesktopPet/actions/runs/45934852652).

The new settings window and diagnostic log still need interactive verification on the user's Windows desktop before M1 can be marked complete.

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

## Local data

- Settings: `%LOCALAPPDATA%\BichonDesktopPet\settings.json`
- Diagnostic log: `%LOCALAPPDATA%\BichonDesktopPet\logs\app.log`

Logs contain application diagnostics and exception details. The log file is created when the first diagnostic event is recorded, so it may not exist during a normal error-free session. The application does not log chat content or microphone input.

## Product and development documents

- [Product definition and MVP roadmap](docs/PRODUCT_DEFINITION_AND_MVP_ROADMAP_V1.md)
- [M1 baseline and acceptance checklist](docs/M1_BASELINE_AND_ACCEPTANCE.md)
- [Project baseline](docs/PROJECT.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Visual specification](docs/VISUAL_SPECIFICATION.md)
- [Runtime asset status](docs/M1_ASSET_UPLOAD.md)

## Development rule

Every milestone must produce a buildable, runnable, testable increment. Do not begin the next milestone until the current one passes its acceptance gate. The current task is M1 only; AI, voice, 3D runtime, arbitrary model import, and a character marketplace are not part of M1.
