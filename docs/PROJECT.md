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

The user confirmed that the previous M1 baseline works normally on their Windows computer. The existing implementation includes:

- Drag the pet window with the left mouse button.
- Click the character to trigger a small reaction.
- Double-click the character or choose “拆开礼物” from the context menu for the gift reaction.
- Toggle snow and decorative lights from the context menu or system tray.
- Restore and hide the pet from the system tray.
- Hide the pet from the context menu; the process remains running.
- Closing the window hides it without terminating the process; exit is available from the tray menu.

## Current M1 implementation

The following M1 additions are now present in `main` and await local Windows verification:

- Dedicated settings window opened from the pet context menu or system tray.
- Always-on-top, snow, decorative lights and optional music controls.
- Music control is disabled with an explanatory message when the audio file is absent.
- Reset pet position to the default lower-right work-area location.
- Changes apply immediately and persist to the existing local JSON settings file.
- Settings persistence moved into a Core service with an injectable path and diagnostic callback, enabling isolated tests.
- Local application log at `%LOCALAPPDATA%\BichonDesktopPet\logs\app.log`.
- Logging for settings read/write failures, UI dispatcher exceptions, application-domain exceptions, unobserved task exceptions, startup failure and image-load failure.
- Automated tests for settings round-trip, missing-file defaults, corrupt-file fallback and save-failure diagnostics.

The user has already confirmed the previous M1 baseline and tray hide/show behavior. This does not count as local validation of the newly added settings window, logging or tests.

## Remaining M1 verification

- Run Release restore/build/test on Windows.
- Open Settings from both the pet context menu and tray menu.
- Toggle each setting and verify immediate behavior and persistence after restart.
- Confirm the music control is unavailable with a clear explanation when no audio file exists.
- Reset position and verify it is persisted.
- Confirm diagnostic log creation and inspect a controlled settings failure.
- Test window position restoration on multiple displays and DPI settings.

## Milestones

- M0: image-based desktop pet display — user-confirmed baseline.
- M1: reliable daily-use image desktop pet — implementation in progress; local verification pending for the latest settings/logging changes.
- M2: multiple image avatars and local avatar management.
- M3: lightweight rule-based behavior system.
- M4: AI text interaction and constrained action mapping.
- M5: push-to-talk voice interaction and speech output.
- M6: single-character real-time 3D technical validation.
- M7: user-imported GLB/glTF static avatar support.
- M8: rig profiles and character-specific animation expansion.
- M9: optional character sharing/ecosystem, only after earlier stages are validated.

See [PRODUCT_DEFINITION_AND_MVP_ROADMAP_V1.md](PRODUCT_DEFINITION_AND_MVP_ROADMAP_V1.md) for the full product definition and release gates.
