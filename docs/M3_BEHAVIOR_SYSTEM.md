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

Windows CI passed on commit `5db5f4dd135f9226665f4defc187c8fe5767ff1d`: Release build completed with 0 warnings and 0 errors; all 30 automated tests passed ([run #124](https://github.com/wenzhong0606-dev/DesktopPet/actions/runs/45946659068)). Local Windows interaction acceptance remains pending.

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


## M3-B — per-avatar identity and personality

M3-B extends the behavior layer with a separate persona profile for each avatar. The avatar remains an image/appearance record; its persona describes who the companion is and how it expresses itself.

### Persona fields

| Field | Meaning | Constraints |
|---|---|---|
| Name | Display name of this avatar | 1–40 characters |
| Species | Broad character type | Existing metadata, up to 24 characters |
| Breed | Breed or subtype | Optional, up to 40 characters |
| Nicknames | Alternative names for this pet | Up to 5, each up to 20 characters |
| SelfReference | How the pet refers to itself | 1–20 characters |
| UserAddress | How the pet addresses the user | 1–20 characters |
| PersonalityTraits | Free-form personality descriptors | Up to 3, each up to 16 characters |
| SpeechStyle | cute, gentle, cool, tsundere, playful, natural | One supported value |
| ResponseLength | short or medium | One supported value |
| EmojiLevel | low, medium or high | One supported value |
| Catchphrases | Optional happy/excited phrases | Up to 5, each up to 40 characters |

### Catalog migration

The avatar catalog is now schema version 2. Loading a valid schema v1 catalog:

- Preserves valid avatar IDs, names, species, image paths and creation timestamps.
- Keeps imported image files in their existing LocalAppData folders.
- Adds safe default persona data to every avatar.
- Keeps the existing \`settings.json\` and \`ActiveAvatarId\` untouched.
- Writes the migrated catalog atomically using the existing temporary-file replacement pattern.

If the catalog is from a newer schema version than this app supports, the service avoids writing it to prevent downgrading newer user data.

### Local dynamic message templates

The behavior resolver now receives the active avatar profile rather than only its species. It uses the persona's self-reference, user address, personality traits, speech style, nicknames, catchphrases, response length and emoji level to select and format messages.

Template selection rotates per avatar and behavior state, so repeated reactions use alternative phrases before cycling back. Species remains a source of interjection/fallback wording, not a substitute for individual personality.

This is still a bounded, deterministic local template system. It is not AI, does not understand arbitrary user input and does not save conversation memory. AI-generated dialogue remains M4.

### M3-B Windows acceptance checklist

- [ ] Existing v1 catalog migrates to v2 without losing custom avatars or image files.
- [ ] Existing selected-avatar setting remains unchanged after migration.
- [ ] Built-in Bichon and imported avatars can be edited independently.
- [ ] Name, breed, nicknames, self-reference and user address save and reload.
- [ ] Personality traits, speech style, response length, emoji level and catchphrases save and reload.
- [ ] More than three traits, too many nicknames/catchphrases, or overlong fields are rejected with a visible validation message.
- [ ] A Bichon configured as lively/affectionate differs from a cat configured as cool/independent.
- [ ] Repeated clicks cycle through different messages before repeating.
- [ ] M3-A behavior and M1/M2 functions still work.
- [ ] No network request or AI configuration is required.

