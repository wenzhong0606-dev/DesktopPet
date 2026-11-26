# Architecture

## Solution layout

- src/BichonDesktopPet.App: WPF executable, views, window lifecycle, platform services, tray icon, audio, and particle rendering.
- src/BichonDesktopPet.Core: platform-independent models and settings contracts.
- tests/BichonDesktopPet.Tests: unit tests for core defaults and behavior.
- assets: source and production character assets, animation frames, audio, and icons.
- docs: product, architecture, and visual specifications.

## Runtime flow

1. App creates the pet window and system tray icon.
2. MainWindow loads local settings and positions itself inside the Windows work area.
3. SnowAnimationService owns a single render-priority dispatcher timer and a bounded particle collection.
4. SettingsService reads and writes JSON defensively; malformed settings never block startup.
5. AudioService looks for an optional authorized audio file and does nothing when it is absent.
6. TrayIconService provides show, snow, lights, music, and exit actions.

## Engineering constraints

- Keep the window lightweight and avoid one timer per particle.
- Keep all animation updates on the WPF dispatcher.
- Bound particle counts and release timers/media resources on exit.
- Keep user settings local; do not add telemetry or network calls.
- Keep original artwork separate from optimized runtime assets.
- Do not commit generated binaries, build outputs, or user-specific settings.
- Avoid adding third-party runtime dependencies unless a concrete requirement justifies them.

## Build and test

Run on Windows with the .NET 10 SDK:

~~~powershell
dotnet restore BichonDesktopPet.sln
dotnet build BichonDesktopPet.sln -c Release
dotnet test BichonDesktopPet.sln -c Release
~~~

The WPF application is Windows-targeted. EnableWindowsTargeting is set so SDK-based builds can restore Windows targeting packs from non-Windows build agents, but runtime validation must be performed on Windows.
