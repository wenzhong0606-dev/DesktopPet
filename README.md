# AI Desktop Companion

**Current repository / engineering project name:** \`BichonDesktopPet\`

AI Desktop Companion is a Windows desktop companion platform developed in small, runnable milestones. The Bichon Frise is the first built-in character, not the only character the platform supports. Users can add their own locally owned image assets and select different desktop companions.

## Current milestone

- **M0 — Image display:** complete.
- **M1 — Daily-use desktop pet:** user acceptance passed.
- **M2 — Multiple image avatars:** first implementation committed; Windows CI passed (Release build, 0 warnings/errors, 17/17 tests); local avatar-manager acceptance is pending.

## Technology

- C# / .NET 10
- WPF (Windows desktop)
- Platform-independent Core library
- Local JSON settings and avatar catalog
- Local image import; no account, network upload, or cloud sync required

## Current implementation

The WPF app retains the transparent 280 × 360 DIP desktop-pet window, Bichon PNG display, vector fallback, dragging, position persistence, click and gift reactions, snow, decorative lights, system tray, settings window, diagnostics, and optional local audio integration.

M2 adds:

- A versioned local avatar catalog with stable avatar IDs.
- The built-in Bichon as the protected default role.
- A role manager accessible from the pet context menu and system tray.
- Import of PNG, JPG, JPEG and BMP images (maximum 20 MB per image).
- Editable display name and simple species metadata.
- Switching between roles, deleting imported roles, and restoring the Bichon default.
- Persistence of the selected avatar ID across application restarts.
- Copying imported images into LocalAppData; source files are not modified.
- Image signature checks and WPF decode validation before import.

Only the Bichon artwork is bundled at this point. No Border Collie or cat artwork is being presented as a built-in asset; users can import images they have permission to use.

## Build and test (Windows)

\`\`\`powershell
git pull origin main
dotnet restore BichonDesktopPet.sln
dotnet build BichonDesktopPet.sln -c Release
dotnet test BichonDesktopPet.sln -c Release
\`\`\`

## Run

\`\`\`powershell
dotnet run --project src/BichonDesktopPet.App/BichonDesktopPet.App.csproj
\`\`\`

## Local data

- Settings: \`%LOCALAPPDATA%\\BichonDesktopPet\\settings.json\`
- Diagnostic log: \`%LOCALAPPDATA%\\BichonDesktopPet\\logs\\app.log\`
- Avatar catalog: \`%LOCALAPPDATA%\\BichonDesktopPet\\avatars\\catalog.json\`
- Imported images: \`%LOCALAPPDATA%\\BichonDesktopPet\\avatars\\{avatar-id}\\avatar.{png|jpg|jpeg|bmp}\`

Imported images remain on this computer. They are not copied into the Git repository or uploaded to a service. Only import artwork that you own or have permission to use.

## Development documents

- [Product definition and MVP roadmap](docs/PRODUCT_DEFINITION_AND_MVP_ROADMAP_V1.md)
- [M1 baseline and acceptance](docs/M1_BASELINE_AND_ACCEPTANCE.md)
- [M2 local avatar management](docs/M2_AVATAR_MANAGEMENT.md)
- [Project baseline](docs/PROJECT.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Visual specification](docs/VISUAL_SPECIFICATION.md)
- [Runtime asset status](docs/M1_ASSET_UPLOAD.md)

## Scope discipline

M2 is limited to local image roles and basic metadata. It does not include 3D import, animation retargeting, AI, voice, accounts, cloud sync, or a character marketplace. Each milestone must build on the user-accepted baseline and remain independently runnable.
