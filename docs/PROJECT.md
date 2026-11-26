# Project Baseline

## Product

BichonDesktopPet is a Windows-only desktop companion. Its mascot is a fluffy white Bichon Frise based on the user's approved visual reference: oversized dark eyes, a small dark nose, blush cheeks, a pink strawberry outfit, and a red festive bow with a gold bell.

## Product decisions

- Target OS: Windows 10 and Windows 11 (x64).
- Runtime: .NET 10.
- UI: WPF.
- Initial window: 280 × 360 device-independent pixels (DIP).
- Window: transparent, borderless, non-resizable, topmost by default, hidden from the taskbar.
- Music: optional and disabled by default.
- Settings: local JSON under the LocalAppData BichonDesktopPet settings folder.
- First visual milestone uses a temporary vector placeholder. The approved 3D character artwork will replace it after asset preparation.

## Initial interaction scope

- Drag the pet window with the left mouse button.
- Click the character to trigger a small reaction.
- Click a gift icon for a gift reaction.
- Toggle snow and decorative lights from the context menu or system tray.
- Hide the window without terminating the process; restore it from the tray.
- Exit from the tray menu.

## Milestones

- M0: repository and WPF skeleton.
- M1: approved Bichon 3D artwork integrated into the character view.
- M2: polished idle, blink, and reaction animations.
- M3: gift interactions, optional audio, and settings UX.
- M4: performance, DPI, multi-monitor, packaging, and release validation.
