# Architecture

## Solution layout

- src/BichonDesktopPet.App: WPF executable, views, window lifecycle, platform services, tray icon, audio, and particle rendering.
- src/BichonDesktopPet.Core: platform-independent models and settings contracts.
- tests/BichonDesktopPet.Tests: unit tests for core defaults and behavior.
- assets: source and production character assets, animation frames, audio, and icons.
- docs: product, architecture, and visual specifications.

## Runtime flow

1. App creates the pet window and system tray icon.
2. MainWindow loads local settings and the versioned avatar catalog, then resolves the selected avatar ID.
3. AvatarCatalogService resolves built-in assets from the packaged Assets folder and imported assets from LocalAppData. If the selected custom avatar is missing, the app falls back to the built-in Bichon.
4. MainWindow decodes the selected image at a bounded display width. If the built-in image is missing or invalid, the temporary vector fallback remains visible and the status label reports the asset state.
5. SnowAnimationService owns a single render-priority dispatcher timer and a bounded particle collection.
6. SettingsService reads and writes JSON defensively; malformed settings never block startup.
7. AvatarCatalogService imports PNG/JPG/JPEG/BMP files into the user's LocalAppData folder, validates extension/signature, protects the built-in Bichon and persists a versioned JSON catalog.
8. AudioService looks for an optional authorized audio file and does nothing when it is absent.
9. TrayIconService provides show, hide, settings, avatar management, snow, lights, music, and exit actions.

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


## Local avatar catalog (M2)

- \`AvatarProfile\` is a platform-independent model with stable ID, display name, species/type metadata, relative image path, built-in flag and creation timestamp.
- \`AvatarCatalogService\` owns \`%LOCALAPPDATA%\\BichonDesktopPet\\avatars\\catalog.json\` and imported image folders.
- Built-in Bichon art resolves from the packaged \`Assets/characters/bichon/base\` directory. Imported art resolves only inside the local avatars directory.
- User images are copied into LocalAppData; the original source file is never modified.
- Supported file types are PNG, JPG, JPEG and BMP, limited to 20 MB per image. The UI performs WPF decode validation before import.
- \`PetSettings.ActiveAvatarId\` stores the current role. If the saved custom role no longer exists, startup restores the built-in Bichon.
- The manager UI is a WPF view; catalog persistence and file validation remain in Core so they can be unit-tested without WPF.
- M2 does not add 3D, animation retargeting, AI, voice, network sync or a marketplace.
