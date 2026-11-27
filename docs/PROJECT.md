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
- No network connection is required for M0–M2 image-based operation.

## M0 and M1 — accepted baseline

The user confirmed that M1 acceptance passed after the settings-window layout fix. The accepted baseline includes:

- Bichon PNG display and vector fallback.
- Dragging and window-position persistence.
- Click and gift reactions.
- Snow and decorative lights.
- System tray show/hide/exit controls.
- Settings window with always-on-top, snow, lights and optional music controls.
- Scrollable settings content with a fixed completion button and resizable window.
- Local JSON settings persistence.
- Local diagnostic logging and defensive recovery.
- Windows Release CI and automated settings tests.

The latest settings-window layout fix passed Windows CI: [run #65](https://github.com/wenzhong0606-dev/DesktopPet/actions/runs/45942365232). The user subsequently confirmed local acceptance.

## M2 — multiple image avatars (implementation in progress)

The first M2 increment is committed to \`main\`:

- \`AvatarProfile\`: stable ID, display name, species/type, image path, built-in flag and creation time.
- \`AvatarCatalogService\`: versioned JSON catalog, built-in Bichon seeding, image import, deletion, path validation and local storage.
- \`AvatarManagerWindow\`: import, list, switch, delete and restore-default UI.
- Main-window and system-tray entry points for role management.
- \`PetSettings.ActiveAvatarId\`: remembers the selected role.
- Imported images are copied into the current user's LocalAppData folder; repository artwork is not modified.
- Supported imports: PNG, JPG, JPEG and BMP; maximum file size 20 MB.
- Import validates extension, file signature and WPF image decoding.
- Imported roles cannot delete the built-in Bichon.

The repository currently bundles only the Bichon artwork. A Border Collie or cat image is not bundled yet; the user can import their own image assets.

### M2 local storage

- Settings: \`%LOCALAPPDATA%\\BichonDesktopPet\\settings.json\`
- Catalog: \`%LOCALAPPDATA%\\BichonDesktopPet\\avatars\\catalog.json\`
- Imported image: \`%LOCALAPPDATA%\\BichonDesktopPet\\avatars\\{avatar-id}\\avatar.{extension}\`

### M2 acceptance still required

- Windows CI Release build and tests pass.
- Open role manager from the pet context menu and system tray.
- Import a valid PNG/JPG/BMP image and see it in the local role list.
- Switch from Bichon to the imported role and back.
- Restart and confirm the selected role is restored.
- Delete an imported role; confirm the built-in Bichon cannot be deleted.
- Delete the currently active custom role and confirm automatic fallback to Bichon.
- Reject unsupported, oversized, signature-mismatched or undecodable files.
- Confirm the existing M1 interactions remain functional.

## Milestones

- M0: image-based desktop pet display — accepted.
- M1: reliable daily-use image desktop pet — user acceptance passed.
- M2: multiple image avatars and local avatar management — implementation in progress.
- M3: lightweight rule-based behavior system.
- M4: AI text interaction and constrained action mapping.
- M5: push-to-talk voice interaction and speech output.
- M6: single-character real-time 3D technical validation.
- M7: user-imported GLB/glTF static avatar support.
- M8: rig profiles and character-specific animation expansion.
- M9: optional character sharing/ecosystem, only after earlier stages are validated.

See [PRODUCT_DEFINITION_AND_MVP_ROADMAP_V1.md](PRODUCT_DEFINITION_AND_MVP_ROADMAP_V1.md) and [M2_AVATAR_MANAGEMENT.md](M2_AVATAR_MANAGEMENT.md) for scope and acceptance criteria.
