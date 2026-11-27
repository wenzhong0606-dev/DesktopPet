# Changelog

## 2026-11-27 — M1 settings and diagnostics

- Add a dedicated settings window, accessible from the pet context menu and system tray.
- Add live controls for always-on-top, snow, decorative lights and optional background music.
- Disable the music control and explain the missing asset when the optional audio file is unavailable.
- Add a reset-to-default-position action.
- Move settings persistence into a Core service with injectable path and diagnostic callback.
- Save settings through a temporary file and replace the previous JSON file after serialization.
- Add local application logging for settings failures, startup failures, image-load failures, and unhandled UI/application/task exceptions.
- Add tests for settings round-trip, missing-file defaults, corrupt JSON fallback and save-failure logging.
- Record that the user confirmed the previous M1 desktop-pet and tray baseline works locally; the new settings window and diagnostics still require Windows verification.

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
