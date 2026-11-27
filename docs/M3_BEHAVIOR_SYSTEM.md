# M3 — Lightweight Behavior System

## Goal

Give each image-based desktop companion a small set of understandable, rule-driven reactions to user actions and inactivity. Keep behavior decisions independent from WPF rendering and from the avatar's image assets.

M3 remains local and deterministic. No AI, network calls, voice, microphone, 3D runtime or cloud service is required.

## First implementation delivered

### Behavior states

| State | Trigger | Duration / exit |
|---|---|---|
| Idle | Default state or transient state expires | Remains until inactivity |
| Greeting | App startup or restore from system tray | 2.6 seconds, then Idle |
| Happy | Single click | 1.7 seconds, then Idle |
| Excited | Gift interaction or three rapid clicks within four seconds | 2.4 seconds, then Idle |
| Sleepy | 60 seconds without interaction | Remains until the next interaction |

When a transient state expires after the inactivity threshold has already elapsed, the engine enters Sleepy rather than briefly returning to Idle.

### Separation of responsibilities

- \`PetBehaviorEngine\` (Core): deterministic state transitions; accepts timestamps from its caller, so it can be tested without a WPF timer.
- \`PetBehaviorPresentationResolver\` (Core): maps state to a short message and a \`PetVisualEffect\`; provides species-aware wording for dog, cat and rabbit roles.
- \`PetBehaviorAnimationService\` (WPF App): renders effects using transforms on the whole static image.
- \`MainWindow\`: owns one one-second DispatcherTimer, forwards user interactions to the engine, applies presentation changes and disposes timer/animation resources on exit.

### Current visual effects

- Idle: subtle breathing scale.
- Greeting: short vertical bounce.
- Happy: two small bounces.
- Excited: short rotation wiggle and scale pulse.
- Sleepy: very subtle slow sway.

These effects transform the entire image. They do not animate independent legs, paws, tail, eyes or mouth. Real dog actions such as rolling, paw-shaking and tail-wagging require suitable frame sequences or a rigged animated model and are not claimed in this increment.

## Automated verification

Core unit tests cover:

- Startup greeting and timed return to idle.
- Click-to-happy and timed return to idle.
- Three rapid clicks to excited.
- Resetting the rapid-click window.
- Gift interaction to excited.
- Inactivity to sleepy and wake by interaction.
- Tray wake greeting and inactivity reset.
- Species-aware presentation messages and visual-effect mapping.

Windows CI passed on commit `e78c94d706d123c1a5d999e494e6f3ca184380d0`: Release build completed with 0 warnings and 0 errors; all 30 automated tests passed ([run #111](https://github.com/wenzhong0606-dev/DesktopPet/actions/runs/45946216141)). Local Windows interaction acceptance remains pending.

## Windows local acceptance checklist

- [ ] Release build and all automated tests pass.
- [ ] Launch the app: greeting appears briefly, then the idle breathing effect starts.
- [ ] Single-click the active character: happy message and bounce appear.
- [ ] Use **拆开礼物** from the context menu: excited wiggle appears.
- [ ] Click the character rapidly three times: excited state appears.
- [ ] Leave the app untouched for 60 seconds: sleepy sway appears.
- [ ] Click the character after Sleepy: it wakes into Happy.
- [ ] Hide to tray and restore: Greeting appears and the inactivity clock resets.
- [ ] Switch among imported dog/cat/rabbit images: state messages use the corresponding species wording.
- [ ] Existing M1/M2 features still work: dragging, snow, lights, settings, tray, avatar import/switch/delete and persistence.
- [ ] Close the app: no behavior timer or animation continues running.

## Known limitations and next M3 increment

- The first increment uses whole-image transforms, not frame-by-frame or skeletal animation.
- No randomized idle behavior has been added yet; this is deferred until the deterministic state machine passes local acceptance.
- No per-avatar custom animation manifest yet; species metadata currently selects wording, while the static-image host uses the same safe transform effects.
- No AI, voice, 3D, rigging or network integration.

After local acceptance, the next M3 increment can add configurable, low-frequency idle variations and optional frame-sequence playback once suitable assets are available.
