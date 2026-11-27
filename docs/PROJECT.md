# Project Baseline

## Product direction

The product is **AI Desktop Companion**, a Windows desktop companion platform. The repository and executable retain the engineering name `BichonDesktopPet` for now; no rename is needed during the MVP phase.

The Bichon Frise is the first sample avatar. The long-term platform may support different animals, stylized human characters, and user-imported legal assets. Different character types will have different rigs, action capabilities, and behavior profiles. This direction does not expand the current milestone scope.

## Current product baseline (M0)

The user has confirmed that image-based desktop pet display is complete. The current implementation uses .NET 10 and WPF.

- Target OS: Windows 10/11 x64.
- Initial window: 280 × 360 device-independent pixels (DIP).
- Window: transparent, borderless, non-resizable, topmost by default, hidden from the taskbar.
- Character display: load `Assets/characters/bichon/base/bichon-idle.png` from the application output directory.
- Runtime PNG: 420 × 540, RGBA, 132,158 bytes; SHA-256 `326d10ace28d53e8a4289b9f103c648ecb79d6b1b32e4a8f76d715793b97d210`.
- Fallback: retain the temporary vector character if the PNG is absent or cannot be decoded.
- Settings: local JSON under the LocalAppData BichonDesktopPet settings folder.
- Music: optional and disabled by default.

## Existing interactions confirmed in main

- Drag the pet window with the left mouse button.
- Click the character to trigger a small reaction.
- Double-click the character or choose “拆开礼物” from the context menu for the gift reaction.
- Toggle snow and decorative lights from the context menu or system tray.
- Restore the pet from the system tray.
- Closing the window hides it without terminating the process; exit is available from the tray menu.

## Current M1 gaps

- Add an explicit hide action to the tray menu.
- Add a lightweight settings window for existing settings (topmost, snow, decorative lights, music) and reset position.
- Improve settings persistence diagnostics and application exception logging.
- Run Release build, tests, and Windows visual/interaction smoke tests on the user's machine. The PNG's dimensions and SHA-256 have now been inspected from the committed binary; visual display still needs Windows verification.

## Milestones

- M0: image-based desktop pet display — user-confirmed baseline.
- M1: reliable daily-use image desktop pet — in progress.
- M2: multiple image avatars and local avatar management.
- M3: lightweight rule-based behavior system.
- M4: AI text interaction and constrained action mapping.
- M5: push-to-talk voice interaction and speech output.
- M6: single-character real-time 3D technical validation.
- M7: user-imported GLB/glTF static avatar support.
- M8: rig profiles and character-specific animation expansion.
- M9: optional character sharing/ecosystem, only after earlier stages are validated.

See [PRODUCT_DEFINITION_AND_MVP_ROADMAP_V1.md](PRODUCT_DEFINITION_AND_MVP_ROADMAP_V1.md) for the full product definition and release gates.
