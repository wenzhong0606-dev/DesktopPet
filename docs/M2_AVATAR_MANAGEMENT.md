# M2 — Local Image Avatar Management

## Goal

Move the app from a single fixed Bichon image to a locally managed set of image-based avatars, while preserving the accepted M1 desktop-pet behavior.

M2 is a local Windows feature. It does not require an account, network access, cloud storage, 3D runtime, AI, voice, or a character marketplace.

## Delivered scope

- Stable avatar IDs and a schema-versioned JSON catalog.
- Built-in Bichon profile seeded on first run and protected from deletion.
- Local image import for PNG, JPG, JPEG and BMP (maximum 20 MB).
- File extension and signature checks in Core; WPF decode validation before copying.
- Editable display name and simple species/type metadata.
- List, switch, delete imported avatars, and restore the Bichon default.
- Persist the selected avatar ID in the existing settings JSON.
- If the selected custom avatar is missing on startup, fall back to Bichon.
- Imported images are copied to LocalAppData; the source file and repository artwork are not modified.
- Role manager is accessible from the desktop pet context menu and Windows system tray.

## User acceptance

**Status: accepted. The user explicitly confirmed: “M2验收通过”.**

All M2 local acceptance checks were confirmed by the user:

- [x] Release restore/build/test passes on Windows.
- [x] Role manager opens from the pet right-click menu.
- [x] Role manager opens from the system tray.
- [x] Import a valid PNG image and see the new role in the list.
- [x] Import a valid JPG or BMP image.
- [x] The source image file remains unchanged after import.
- [x] Switch from Bichon to an imported role; the desktop pet displays that image.
- [x] Restart the app and confirm the selected role is restored.
- [x] Switch back using **恢复比熊默认**.
- [x] Delete an imported non-active role.
- [x] Delete the active custom role and confirm fallback to Bichon.
- [x] Confirm the built-in Bichon cannot be deleted.
- [x] Reject unsupported, oversized, signature-mismatched and undecodable files.
- [x] Confirm dragging, click/gift reaction, snow, lights, settings, tray hide/show and exit still work.

Windows CI for the M2 baseline passed Release build and all 17 automated tests: [run #97](https://github.com/wenzhong0606-dev/DesktopPet/actions/runs/45944152242).

## Local storage

- User settings: \`%LOCALAPPDATA%\\BichonDesktopPet\\settings.json\`
- Avatar catalog: \`%LOCALAPPDATA%\\BichonDesktopPet\\avatars\\catalog.json\`
- Imported images: \`%LOCALAPPDATA%\\BichonDesktopPet\\avatars\\{avatar-id}\\avatar.{png|jpg|jpeg|bmp}\`

The catalog uses schema version 1. The selected avatar ID is stored separately in \`settings.json\`.

Built-in image paths are relative to the packaged \`Assets\` directory. Imported image paths are relative to the local \`avatars\` directory. Path normalization prevents catalog entries from resolving outside their respective roots.

## Current bundled artwork

Only the Bichon image is bundled. There is no bundled Border Collie or cat illustration in this increment. Users can import artwork they own or have permission to use. Imported images stay on the local computer and are not committed to Git.

## Verification record

- Windows Release build passed.
- 17/17 automated tests passed on commit \`87e7c5225491b46d851d403738afb48655b9f02a\`: [GitHub Actions run #97](https://github.com/wenzhong0606-dev/DesktopPet/actions/runs/45944152242).
- User completed local interaction acceptance and confirmed M2 passed.

## Known limitations

- Only static images are supported; animated GIF and frame sequences are not part of M2.
- No built-in Border Collie or cat artwork is supplied.
- No per-avatar animation or behavior profiles were included in M2; the first behavior-state increment belongs to M3.
- No 3D models, rigging, animation retargeting, AI or voice.
- There is no cloud backup or synchronization. Back up the local avatar folder manually if needed.
