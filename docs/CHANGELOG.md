# Changelog

## 2026-11-27 — Product definition and M1 baseline audit

- Define AI Desktop Companion as a multi-avatar Windows desktop companion platform; Bichon remains the first sample avatar.
- Add the product definition, staged MVP roadmap, and M1 baseline/acceptance checklist.
- Confirm from the current main branch that PNG loading, dragging, click/gift reactions, snow, lights, tray integration, and local JSON settings already exist.
- Correct the runtime asset status: `bichon-idle.png` is present in the repository; its committed file size differs from the older prepared derivative record and needs local hash verification.
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
