# Project Baseline

## Product direction

The product is **AI Desktop Companion**, a Windows desktop companion platform. The repository and executable retain the engineering name \`BichonDesktopPet\` for now; no rename is needed during the MVP phase.

The Bichon Frise is the first built-in avatar. The long-term platform may support different animals, stylized human characters, and user-imported legal assets. Different character types will eventually have different rigs, action capabilities, and behavior profiles.

## Platform baseline

- Target OS: Windows 10/11 x64.
- Technology: .NET 10 / WPF.
- Desktop-pet window: transparent, borderless, 280 × 360 DIP, topmost by default.
- Character presentation: local PNG images with a vector fallback.
- User settings and avatar catalog: local JSON under LocalAppData.
- M0–M3 behavior operation is local and does not require an AI/network service.

## M0 and M1 — accepted baseline

The user confirmed M1 acceptance after the settings-window layout fix. The baseline includes Bichon PNG display, vector fallback, dragging, position persistence, click/gift feedback, snow, decorative lights, system tray, settings, local persistence and diagnostics.

The settings-window layout fix passed Windows CI: [run #65](https://github.com/wenzhong0606-dev/DesktopPet/actions/runs/45942365232). The user subsequently confirmed local acceptance.

## M2 — multiple image avatars (accepted)

**Status: user acceptance passed.**

The implementation includes:

- \`AvatarProfile\`: stable ID, display name, species/type, image path, built-in flag and creation time.
- \`AvatarCatalogService\`: versioned JSON catalog, built-in Bichon seeding, image import, deletion, path validation and local storage.
- \`AvatarManagerWindow\`: import, list, switch, delete and restore-default UI.
- Main-window and system-tray entry points for role management.
- \`PetSettings.ActiveAvatarId\`: remembers the selected role.
- PNG/JPG/JPEG/BMP import, up to 20 MB per image, with extension/signature checks and WPF decode validation.
- Imported images are copied into the current user's LocalAppData folder; repository artwork is not modified.
- The built-in Bichon cannot be deleted.

The user explicitly confirmed M2 acceptance. Windows CI for the M2 baseline passed Release build and all 17 automated tests: [run #97](https://github.com/wenzhong0606-dev/DesktopPet/actions/runs/45944152242).

Only Bichon artwork is bundled. Other roles are added by local import.

## M3-A — lightweight behavior system (local acceptance pending)

The first implementation is committed to \`main\`. Windows CI passed on commit `5db5f4dd135f9226665f4defc187c8fe5767ff1d`: Release build with 0 warnings and 0 errors, 30/30 tests passed ([run #124](https://github.com/wenzhong0606-dev/DesktopPet/actions/runs/45946659068)).

The implementation includes:

- \`PetBehaviorEngine\` is platform-independent and deterministic; callers provide timestamps so state transitions can be unit-tested without WPF timers.
- States: Idle, Greeting, Happy, Excited and Sleepy.
- Startup and tray restore trigger Greeting.
- Single clicks trigger Happy; gift interactions and three rapid clicks trigger Excited.
- Transient states return to Idle after their duration. Sixty seconds without interaction transitions to Sleepy.
- Interaction wakes the companion; tray restore triggers Greeting and resets inactivity.
- \`PetBehaviorPresentationResolver\` maps state to a status message and a separate visual-effect identifier, with species-aware wording for dog, cat and rabbit.
- The WPF host renders subtle whole-image breathing, bounce, wiggle and sleepy sway effects.
- The behavior timer is owned by MainWindow and stopped/disposed during application exit.
- Unit tests cover transitions, timing, repeated clicks, wake behavior and species presentation mapping.

**Honest visual limitation:** current role assets are static images. M3 first increment transforms the whole image; it does not animate individual limbs, tail, eyes or mouth. Real frame-sequence or rigged animation is a later increment after suitable assets are supplied.

### M3 first-increment acceptance still required

- Windows Release build and all automated tests pass.
- Startup shows a brief greeting, then idle breathing.
- Single click shows a happy response.
- Gift interaction and rapid repeated clicks show an excited response.
- After 60 seconds without interaction the role enters sleepy sway.
- Clicking or restoring from the tray wakes the role.
- Imported dog/cat/rabbit roles display the corresponding status wording.
- M1/M2 functions remain intact: drag, snow, lights, settings, tray, avatar import/switch/delete and persistence.
- No sustained CPU usage or animation continues after application exit.

## M3-B — per-avatar identity and persona system (implementation committed; CI/local acceptance pending)

The first vertical slice is committed to \`main\`:

- Add a separate \`PersonaProfile\` attached to each \`AvatarProfile\`, rather than mixing personality fields into the behavior engine.
- Add optional \`Breed\` metadata while keeping \`Species\` as the broader type.
- Upgrade the local avatar catalog from schema v1 to v2. Existing avatar IDs, image paths, image files, names, species and creation timestamps are preserved; existing settings and selected-avatar ID are not rewritten by the migration.
- Migrate missing persona data to safe defaults. Preserve customized built-in Bichon identity/persona on subsequent loads.
- Protect future catalog versions from being overwritten: a catalog created by a newer schema is opened read-only by this version.
- Add a WPF editor reachable from the role manager for built-in and imported avatars.
- Configure pet nicknames, self-reference, user address, up to three free-form personality traits, speech style, response length, emoji level and up to five catchphrases.
- Validate profile lengths/counts and persist changes atomically through the existing catalog save path.
- Connect MainWindow to the active avatar's persona. The local resolver rotates through bounded templates per avatar/state and adjusts tone using speech style, personality traits, nicknames, catchphrases, response length and emoji preference.
- Keep the existing species-only resolver overload as a compatibility fallback.

**Scope boundary:** this is deterministic local template selection, not AI conversation. It does not understand arbitrary user text, retain dialogue memory, or make network requests.

### M3-B acceptance still required

- Windows Release build and all automated tests pass.
- Existing v1 catalog migrates to v2 without losing avatar IDs, source images, image paths or selected-avatar settings.
- Built-in and imported avatars can each save and reload their own persona.
- Nicknames, self-reference, user address, traits, speech style, response length, emoji level and catchphrases validate and persist.
- More than three traits or invalid field lengths are rejected without corrupting the previous profile.
- Different avatars produce distinct messages; repeated same-state reactions rotate through alternatives before repeating.
- M3-A behavior states/effects and M1/M2 desktop functions remain intact.
- No AI, voice, 3D, network access or cloud storage is introduced.

## Local storage

- Settings: \`%LOCALAPPDATA%\\BichonDesktopPet\\settings.json\`
- Avatar catalog: \`%LOCALAPPDATA%\\BichonDesktopPet\\avatars\\catalog.json\`
- Imported image: \`%LOCALAPPDATA%\\BichonDesktopPet\\avatars\\{avatar-id}\\avatar.{extension}\`

## Milestones

- M0: image-based desktop pet display — accepted.
- M1: reliable daily-use image desktop pet — user acceptance passed.
- M2: multiple image avatars and local avatar management — user acceptance passed.
- M3: rule-based behavior and lightweight image effects — first increment in progress.
- M4: AI text interaction and constrained action mapping.
- M5: push-to-talk voice interaction and speech output.
- M6: single-character real-time 3D technical validation.
- M7: user-imported GLB/glTF static avatar support.
- M8: rig profiles and character-specific animation expansion.
- M9: optional character sharing/ecosystem, only after earlier stages are validated.

See [PRODUCT_DEFINITION_AND_MVP_ROADMAP_V1.md](PRODUCT_DEFINITION_AND_MVP_ROADMAP_V1.md), [M2_AVATAR_MANAGEMENT.md](M2_AVATAR_MANAGEMENT.md) and [M3_BEHAVIOR_SYSTEM.md](M3_BEHAVIOR_SYSTEM.md).
