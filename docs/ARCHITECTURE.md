# Architecture

## Solution layout

- src/BichonDesktopPet.App: WPF executable, views, window lifecycle, platform services, tray icon, audio, and particle rendering.
- src/BichonDesktopPet.Core: platform-independent models and settings contracts.
- tests/BichonDesktopPet.Tests: unit tests for core defaults and behavior.
- assets: source and production character assets, animation frames, audio, and icons.
- docs: product, architecture, and visual specifications.

## Runtime flow

1. App creates the pet window and system tray icon.
2. MainWindow loads local settings and attempts to load the optimized Bichon PNG from Assets/characters/bichon/base/bichon-idle.png.
3. If the runtime image is missing or invalid, the temporary vector fallback remains visible and the status label reports the asset state.
4. SnowAnimationService owns a single render-priority dispatcher timer and a bounded particle collection.
5. SettingsService reads and writes JSON defensively; malformed settings never block startup.
6. AudioService looks for an optional authorized audio file and does nothing when it is absent.
7. TrayIconService provides show, snow, lights, music, and exit actions.

## Engineering constraints

- Keep the window lightweight and avoid one timer per particle.
- Keep all animation updates on the WPF dispatcher.
- Bound particle counts and release timers/media resources on exit.
- Keep user settings local; do not add telemetry or network calls.
- Keep original artwork separate from optimized runtime assets.
- Do not commit generated binaries, build outputs, or user-specific settings.
- Avoid adding third-party runtime dependencies unless a concrete requirement justifies it.

## Character asset loading

The app uses a content file copied beside the executable. BitmapImage is initialized with BitmapCacheOption.OnLoad, then frozen so the source file can be released and the image can be used safely by WPF rendering.

The expected repository path is assets/characters/bichon/base/bichon-idle.png. The project file's existing content wildcard copies the PNG to Assets/characters/bichon/base/ in the build output.

## Build and test

Run on Windows with the .NET 10 SDK:

~~~powershell
dotnet restore BichonDesktopPet.sln
dotnet build BichonDesktopPet.sln -c Release
dotnet test BichonDesktopPet.sln -c Release
~~~

The WPF application is Windows-targeted. EnableWindowsTargeting is set so SDK-based builds can restore Windows targeting packs from non-Windows build agents, but runtime visual validation must be performed on Windows.
