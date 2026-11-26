# Project Baseline

## Product

BichonDesktopPet is a Windows-only desktop companion. Its mascot is a fluffy white Bichon Frise based on the user's uploaded visual reference: oversized dark eyes, a small dark nose, blush cheeks, a pink gingham strawberry outfit, and a red festive bow with a gold bell.

## Product decisions

- Target OS: Windows 10 and Windows 11 (x64).
- Runtime: .NET 10.
- UI: WPF.
- Initial window: 280 × 360 device-independent pixels (DIP).
- Window: transparent, borderless, non-resizable, topmost by default, hidden from the taskbar.
- Music: optional and disabled by default.
- Settings: local JSON under the LocalAppData BichonDesktopPet settings folder.
- Character display: load Assets/characters/bichon/base/bichon-idle.png from the application output directory.
- Fallback: retain the temporary vector character if the optimized PNG is absent or cannot be decoded.

## Initial interaction scope

- Drag the pet window with the left mouse button.
- Click the character to trigger a small reaction.
- Double-click the character or choose “拆开礼物” from the context menu for the gift reaction.
- Toggle snow and decorative lights from the context menu or system tray.
- Hide the window without terminating the process; restore it from the tray.
- Exit from the tray menu.

## Milestones

- M0: repository and WPF skeleton — complete.
- M1: integrate the user's 3D Bichon artwork — image-loading code committed; runtime PNG upload and Windows visual verification remain.
- M2: polished idle, blink, and reaction animations.
- M3: gift interactions, optional audio, and settings UX.
- M4: performance, DPI, multi-monitor, packaging, and release validation.
