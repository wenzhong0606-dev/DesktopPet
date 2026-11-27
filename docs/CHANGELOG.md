# Changelog

## 2026-11-27 — M4-A first chat vertical slice

- Add an explicit WPF chat window reachable from the desktop-pet context menu and system tray.
- Reuse the provider-neutral Qwen `IAiChatProvider`; no request is sent at application startup.
- Build bounded system context from the active avatar persona and current behavior state, plus up to 12 recent in-window user/assistant messages.
- Keep conversation content in window memory only; do not persist chat text to the interaction journal or SQLite.
- Disable sending and show local setup guidance when the Qwen API key is not configured; offline pet functions remain available.
- Handle asynchronous requests, cancellation/timeout and provider/network failures without logging conversation text.
- Add unit tests for persona context, history filtering and message-size bounds. Windows CI and local UI acceptance are pending.


## 2026-11-27 — Correct delayed single/double-click recognition

- Delay writing \`pet_clicked\` until the Windows double-click interval expires.
- Recognize a double-click using the Windows time and pointer-distance thresholds; emit \`pet_double_clicked\` and \`gift_opened\` without emitting either constituent single-click event.
- Keep two clicks outside the system time or position threshold as two independent single clicks.
- Flush a pending single click before hiding, switching/deleting an avatar, or explicitly exiting so real clicks are not lost.
- Stop and detach the click timer during application shutdown.
- Add platform-independent regression tests for single, double, separate, spatially distinct and pending-click lifecycle cases.

## 2026-11-27 — M3-C interaction lifecycle event coverage

- Record double-click gestures separately as pet_double_clicked, while retaining gift_opened for the resulting gift action.
- Distinguish gift opening from double-click and context-menu sources in the event payload.
- Record hidden_to_tray for explicit hide actions and window-close gestures that are converted into tray hiding.
- Record application_exited when the user explicitly exits through the application exit command.
- Keep tray wake events and single-click events as separate timestamped rows.
- Add persistence coverage for the complete interaction lifecycle event vocabulary.

## 2026-11-27 — M3-C companion identity and Qwen provider foundation

- Separate persistent \`CompanionIdentity\` IDs from the avatar currently used to display each companion.
- Add a local SQLite identity store and event journal under LocalAppData.
- Record only local pet-click, gift and tray-wake metadata; do not store raw conversation text.
- Archive removed avatar identities while retaining their interaction history.
- Add provider-neutral \`IAiChatProvider\` and Qwen Chat Completions adapter using the exact \`qwen-plus\` model and the user's configured DashScope endpoint.
- Load API keys from \`Qwen__ApiKey\` or a local ignored settings file; environment variables take precedence.
- Do not send AI requests during startup; no chat UI or memory extraction is included yet.
- Add automated tests for SQLite persistence, event journaling, Qwen config precedence, request construction, response parsing and missing-key behavior.
- Use Microsoft.Data.Sqlite 10.0.15 to avoid the vulnerable older SQLite native dependency chain.
- Pass Windows Release CI with 0 warnings, 0 errors and 61/61 tests on commit `527edd58cec592d7c3a43775c587d151c26a1c44` ([run #224](https://github.com/wenzhong0606-dev/DesktopPet/actions/runs/45960178698)); local Windows acceptance pending.

## 2026-11-27 — M3-B per-avatar persona system

- Add a separate \`PersonaProfile\` to every avatar, including nicknames, self-reference, user address, personality traits, speech style, response length, emoji level and catchphrases.
- Add optional breed/subtype metadata while retaining species as the broad role type.
- Migrate the local avatar catalog from schema v1 to v2 with default personas, preserving avatar IDs, image paths, local image files and the separate selected-avatar setting.
- Avoid downgrading catalogs written by a newer schema version.
- Add a WPF persona editor for built-in and imported avatars.
- Validate persona field lengths and list limits (3 traits, 5 nicknames, 5 catchphrases).
- Connect the active avatar persona to deterministic local message templates with per-avatar/state rotation.
- Keep AI, free-form language understanding, dialogue memory, voice and network access out of M3.
- Add migration, persistence, validation and persona-aware message tests.
- Keep personality traits independent from speech style and make nicknames/catchphrases available across speech styles.
- Pass Windows Release CI with 0 warnings, 0 errors and 47/47 tests on code commit `30d6ae0139335c09b5bcfc6926fc6df43e55acb9` ([run #179](https://github.com/wenzhong0606-dev/DesktopPet/actions/runs/45950812579)); local acceptance pending.


## 2026-11-27 — M3 behavior system (first increment)

- Add a deterministic, platform-independent behavior state engine for Idle, Greeting, Happy, Excited and Sleepy.
- Map startup/tray wake, pet clicks, gift interactions, rapid repeated clicks and 60-second inactivity to state transitions.
- Add species-aware status messages and a separate state-to-visual-effect resolver.
- Add a WPF animation adapter for subtle whole-image breathing, bounce, wiggle and sleepy sway.
- Add a single MainWindow-owned state timer and dispose it and the animation adapter on exit.
- Add automated tests for timed transitions, rapid clicks, gift interaction, inactivity, wake and species presentation mapping.
- Pass Windows Release CI with 0 warnings, 0 errors and 30/30 tests on commit `5db5f4dd135f9226665f4defc187c8fe5767ff1d` ([run #124](https://github.com/wenzhong0606-dev/DesktopPet/actions/runs/45946659068)).
- M2 formally marked accepted after the user's confirmation.


## 2026-11-27 — M2 local avatar management (first increment)

- Add a versioned local avatar catalog and a stable `AvatarProfile` model.
- Seed the existing Bichon artwork as the protected built-in default avatar.
- Add a role manager window reachable from the pet context menu and system tray.
- Support local PNG/JPG/JPEG/BMP imports up to 20 MB, with extension/signature checks and WPF decode validation.
- Copy imported images into LocalAppData without modifying the source file or repository assets.
- Add role names and species/type metadata, switching, deletion of imported roles, and restoring the Bichon default.
- Persist the selected avatar ID and restore it on application startup; fall back to Bichon if a selected custom role is missing.
- Add Core unit tests for catalog seeding, import/reload, invalid files, protected default role, deletion, oversized-file rejection, legacy settings compatibility and corrupt-catalog recovery.
- Pass Windows CI Release build with 0 warnings and 0 errors; all 17 automated tests pass on commit `87e7c5225491b46d851d403738afb48655b9f02a` ([run #97](https://github.com/wenzhong0606-dev/DesktopPet/actions/runs/45944152242)).
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
