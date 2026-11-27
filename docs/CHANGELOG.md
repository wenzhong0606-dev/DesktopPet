# Changelog

## 2026-11-27 — M2 local avatar management (first increment)

- Add a versioned local avatar catalog and a stable `AvatarProfile` model.
- Seed the existing Bichon artwork as the protected built-in default avatar.
- Add a role manager window reachable from the pet context menu and system tray.
- Support local PNG/JPG/JPEG/BMP imports up to 20 MB, with extension/signature checks and WPF decode validation.
- Copy imported images into LocalAppData without modifying the source file or repository assets.
- Add role names and species/type metadata, switching, deletion of imported roles, and restoring the Bichon default.
- Persist the selected avatar ID and restore it on application startup; fall back to Bichon if a selected custom role is missing.
- Add Core unit tests for catalog seeding, import/reload, invalid files, protected default role, deletion and corrupt-catalog recovery.
- M1 marked complete after the user confirmed local acceptance.


## 2026-11-27 — Settings window layout fix

- Make the settings content area vertically scrollable so all options and the reset-position action remain reachable on shorter displays and at higher DPI scaling.
- Keep the completion button in a fixed footer outside the scroll area.
- Increase the default settings-window size and allow resizing, with a minimum size to preserve the layout.

## 2026-11-27 — M1 settings and diagnostics

- Add a dedicated settings window, accessible from the pet context menu and system tray.
- Add live controls for always-on-top, snow, decorative lights and optional background music.
- Disable the music control and explain the missing asset when the optional audio file is unavailable.
- Add a reset-to-default-position action.
- Move settings persistence into a Core service with injectable path and diagnostic callback.
- Save settings through a temporary file and replace the previous JSON file after serialization.
- Return persistence success/failure to the UI so save failures are not presented as successful saves.
- Add local application logging for settings failures, startup failures, image-load failures, and unhandled UI/application/task exceptions.
- Add tests for settings round-trip, missing-file defaults, corrupt JSON fallback and save-failure logging.
- Pass Windows CI Release build with 0 warnings and 0 errors; all 7 tests pass on commit `a6dbd3ea9bda607d2d19b224d4975e9647cec205` ([run #58](https://github.com/wenzhong0606-dev/DesktopPet/actions/runs/45934852652)).
- Record that the user confirmed the previous M1 desktop-pet and tray baseline works locally; the new settings window and diagnostics still require local interactive verification.

## 2026-11-27 — M1 implementation progress

- Add an explicit “hide Bichon” action to the pet context menu and system tray menu; hiding keeps the application running and the tray can restore it.
- Correct the missing-image status text so it no longer claims a 3D asset is pending.
- Update the M1 checklist to distinguish code implementation from Windows runtime verification.

## 2026-11-27 — Product definition and M1 baseline audit

- Define AI Desktop Companion as a multi-avatar Windows desktop companion platform; Bichon remains the first sample avatar.
- Add the product definition, staged MVP roadmap, and M1 baseline/acceptance checklist.
- Confirm from the current main branch that PNG loading, dragging, click/gift reactions, snow, lights, tray integration, and local JSON settings already exist.
- Verify the committed runtime PNG as 420 × 540 RGBA, 132,158 bytes, SHA-256 `326d10ace28d53e8a4289b9f103c648ecb79d6b1b32e4a8f76d715793b97d210`.
- Keep M1 limited to daily-use image desktop pet improvements. AI, voice, 3D, and user model import remain future milestones.

## Unreleased

- Add image-based Bichon character loading with a transparent PNG runtime asset path.
- Retain the temporary vector character as a fallback when the PNG is absent or invalid.
- Move gift interaction to character double-click and the context menu so the full-body image is unobstructed.
- Keep decorative lights behind the character and retain the snow overlay.
- Record source-image provenance and runtime derivative metadata.
- Initialize the .NET 10 WPF solution and shared Core library.
- Add a transparent, draggable 280 × 360 DIP pet window.
- Add bounded snow particles and decorative light pulsing.
- Add tray controls, local settings persistence, and optional audio integration point.
- Add core settings tests and Windows CI workflow.
