# M2 — Local Image Avatar Management

## Goal

Move the app from a single fixed Bichon image to a locally managed set of image-based avatars, while preserving the accepted M1 desktop-pet behavior.

M2 is a local Windows feature. It does not require an account, network access, cloud storage, 3D runtime, AI, voice, or a character marketplace.

## First implementation

- Stable avatar IDs and a schema-versioned JSON catalog.
- Built-in Bichon profile seeded on first run and protected from deletion.
- Local image import for PNG, JPG, JPEG and BMP.
- Maximum imported file size: 20 MB.
- File extension and signature checks in Core; WPF decode validation before copying.
- Editable display name and simple species/type metadata.
- List, switch, delete imported avatars, and restore the Bichon default.
- Persist the selected avatar ID in the existing settings JSON.
- If the selected custom avatar is missing on startup, fall back to Bichon.
- Downsample images for desktop display to reduce memory use.

## User interface

Open **角色管理...** from either:

- The desktop pet's right-click menu.
- The Windows system-tray menu.

In the manager:

1. Select an image file using **导入图片...**.
2. Enter a role name (if blank, the file name is used).
3. Select a simple type: dog, cat, rabbit, reptile, humanoid, or custom.
4. The image is copied into the local avatar directory and appears in the list.
5. Select the new role and choose **设为当前角色**.
6. Use **恢复比熊默认** to return to the built-in Bichon.
7. Imported roles can be deleted. The built-in Bichon cannot be deleted.
8. Deleting the active custom role switches back to Bichon.

## Local storage

- User settings: \`%LOCALAPPDATA%\\BichonDesktopPet\\settings.json\`
- Avatar catalog: \`%LOCALAPPDATA%\\BichonDesktopPet\\avatars\\catalog.json\`
- Imported images: \`%LOCALAPPDATA%\\BichonDesktopPet\\avatars\\{avatar-id}\\avatar.{png|jpg|jpeg|bmp}\`

The catalog uses schema version 1. The selected avatar ID is stored separately in \`settings.json\`.

Built-in image paths are relative to the packaged \`Assets\` directory. Imported image paths are relative to the local \`avatars\` directory. Path normalization prevents catalog entries from resolving outside their respective roots.

## Current bundled artwork

Only the Bichon image is bundled in the repository. There is no bundled Border Collie or cat illustration in this increment. Users can import artwork they own or have permission to use. Imported images stay on the local computer and are not committed to Git.

## Automated tests

The Core tests cover:

- First-run catalog seeding.
- Import and persistence across service reload.
- Extension/signature mismatch rejection.
- Unsupported extension rejection.
- Protection of the built-in Bichon.
- Deletion of an imported avatar.
- Recovery from a corrupt catalog.
- Persistence of the selected avatar ID in settings.
- Default active avatar remains Bichon for new settings.

## Windows acceptance checklist

- [ ] Release restore/build/test passes on Windows.
- [ ] Role manager opens from the pet right-click menu.
- [ ] Role manager opens from the system tray.
- [ ] Import a valid PNG image and see the new role in the list.
- [ ] Import a valid JPG or BMP image.
- [ ] The source image file remains unchanged after import.
- [ ] Switch from Bichon to an imported role; the desktop pet displays that image.
- [ ] Restart the app and confirm the selected role is restored.
- [ ] Switch back using **恢复比熊默认**.
- [ ] Delete an imported non-active role.
- [ ] Delete the active custom role and confirm fallback to Bichon.
- [ ] Confirm the built-in Bichon cannot be deleted.
- [ ] Reject unsupported, oversized, signature-mismatched and undecodable files.
- [ ] Confirm dragging, click/gift reaction, snow, lights, settings, tray hide/show and exit still work.

## Known limitations

- Only static images are supported; animated GIF and frame sequences are not part of this increment.
- No built-in Border Collie or cat artwork is supplied yet.
- No per-avatar animation or behavior profiles yet; those belong to M3.
- No 3D models, rigging, animation retargeting, AI or voice.
- There is no cloud backup or synchronization. Back up the local avatar folder manually if needed.

