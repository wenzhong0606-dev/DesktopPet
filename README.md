# AI Desktop Companion

**Current repository / engineering project name:** \`BichonDesktopPet\`

AI Desktop Companion is a Windows desktop companion platform developed in small, runnable milestones. The Bichon Frise is the first built-in character, not the only character the platform supports. Users can add their own locally owned image assets and select different desktop companions.

## Current milestone

- **M0 — Image display:** complete.
- **M1 — Daily-use desktop pet:** user acceptance passed.
- **M2 — Multiple image avatars:** user acceptance passed.
- **M3 — Lightweight behavior system:** M3-A passed Windows Release CI (30/30 tests); M3-B passed Windows Release CI (45/45 tests, 0 warnings/errors); local acceptance is pending. [M3-B CI run #160](https://github.com/wenzhong0606-dev/DesktopPet/actions/runs/45949927255)

## Technology

- C# / .NET 10
- WPF (Windows desktop)
- Platform-independent Core library
- Local JSON settings and avatar catalog
- Local image import; no account, network upload, or cloud sync required

## Current implementation

The WPF app retains the transparent 280 × 360 DIP desktop-pet window, Bichon PNG display, vector fallback, dragging, position persistence, system tray, settings, snow, decorative lights and optional local audio integration.

M2 provides:

- A versioned local avatar catalog with stable avatar IDs.
- The built-in Bichon as the protected default role.
- A role manager accessible from the pet context menu and system tray.
- Import of PNG, JPG, JPEG and BMP images (maximum 20 MB per image).
- Editable display name and simple species metadata.
- Switching, deleting imported roles, restoring the Bichon default and persistence across restarts.
- Local-only imported image storage and image validation.

M3-B adds:

- A separate `PersonaProfile` for each avatar, plus optional breed/subtype metadata.
- A WPF editor for names, nicknames, self-reference, user address, up to three traits, speech style, response length, emoji level and catchphrases.
- Avatar catalog schema v2 with non-destructive v1 migration; selected-avatar settings and imported image files remain untouched.
- Deterministic persona-aware message templates with per-avatar/state rotation. This is not AI conversation.

M3 first increment provides:

- A platform-independent behavior state engine: Idle, Greeting, Happy, Excited and Sleepy.
- Rule-based transitions for startup/wake, clicks, gift interactions, rapid repeated clicks and inactivity.
- Persona-aware rotating behavior messages using each avatar's identity, traits and speech preferences, with species-based fallback.
- A separate presentation resolver mapping behavior states to WPF image effects.
- Subtle whole-image breathing, bounce, wiggle and sleepy sway effects.
- Unit tests for state transitions and presentation mapping.

Only the Bichon artwork is bundled. Users can import other images they own or have permission to use. M3 currently animates the whole static image; it does not claim skeletal movement, tail wagging or limb animation.

## Build and test (Windows)

~~~powershell
git pull origin main
dotnet restore BichonDesktopPet.sln
dotnet build BichonDesktopPet.sln -c Release
dotnet test BichonDesktopPet.sln -c Release
~~~

## Run

~~~powershell
dotnet run --project src/BichonDesktopPet.App/BichonDesktopPet.App.csproj
~~~

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
- [M3 behavior system](docs/M3_BEHAVIOR_SYSTEM.md)
- [M3-B persona system](docs/M3B_PERSONA_SYSTEM.md)
- [Project baseline](docs/PROJECT.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Visual specification](docs/VISUAL_SPECIFICATION.md)

## Scope discipline

M3 remains a local, rule-based image behavior feature. Persona-aware text is generated from bounded local templates; it is not AI-generated conversation and does not retain chat memory. M3 does not include voice, 3D models, rigging, cloud sync, accounts or a character marketplace. Each milestone must build on the user-accepted baseline and remain independently runnable.
