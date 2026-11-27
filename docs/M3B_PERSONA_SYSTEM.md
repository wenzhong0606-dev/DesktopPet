# M3-B — Per-avatar Identity and Persona System

## Goal

Make every desktop companion an individually configurable character, not merely a replaceable image. Each avatar has a management label for the role list, plus a separate primary pet nickname, self-reference, user address, personality and communication preferences.

M3-B remains local and deterministic. It does not add AI, free-form language understanding, dialogue memory, voice, 3D or network calls.

## Data model

\`AvatarProfile\` retains its stable ID, display name, species, image path, built-in flag and creation timestamp. It now also has:

- \`Breed\`: optional breed or character subtype, distinct from broad \`Species\`.
- \`Persona\`: an independent \`PersonaProfile\`.

\`PersonaProfile\` contains:

| Field | Description | Validation |
|---|---|---|
| Nicknames | Ordered pet names: first entry is the primary conversational name; later entries are aliases | 0–5 items; 1–20 chars each |
| SelfReference | Pet's self-reference | Required, 1–20 chars |
| UserAddress | How the pet addresses the user | Required, 1–20 chars |
| PersonalityTraits | Free-form traits such as 活泼、黏人、好奇 | 0–3 items; 1–16 chars each. Current rules recognize common traits; custom labels are stored but do not yet add specialized templates. |
| SpeechStyle | cute / gentle / cool / tsundere / playful / natural | Supported value only |
| ResponseLength | short / medium | Supported value only |
| EmojiLevel | low / medium / high | Supported value only |
| Catchphrases | Custom phrases used for happy/excited states | 0–5 items; 1–40 chars each |

The existing \`AvatarProfile.Name\` remains the primary display name. A nickname is an additional name, while \`SelfReference\` controls the first-person reference used in generated messages.

## WPF workflow

1. Open **角色管理** from the desktop pet or tray.
2. Select any built-in or imported avatar.
3. Choose **编辑资料与个性**.
4. Edit the management label, species, breed, ordered pet nicknames (first is the primary name), self-reference, user address, personality traits and expression preferences.
5. Save. The editor validates the data and persists it to the local avatar catalog.

The built-in Bichon may be renamed and personalized, but its protected image and stable ID remain unchanged. Imported avatars can be edited independently.

## Catalog schema v1 → v2 migration

The catalog schema is upgraded from version 1 to version 2.

- Existing valid avatar IDs, display names, species, relative image paths and creation timestamps are retained.
- Existing imported image files and avatar folders are not moved or renamed.
- Each avatar without persona data receives safe default persona settings.
- Existing \`settings.json\` and \`PetSettings.ActiveAvatarId\` are separate from the avatar catalog and are not rewritten by this migration.
- The migrated catalog is saved using the existing temporary-file/replace strategy.
- A catalog whose schema version is newer than the current app is treated as read-only, preventing this version from overwriting newer data.

## Message generation

\`PetBehaviorPresentationResolver\` now accepts an \`AvatarProfile\`. It combines behavior state, species, and persona preferences to choose a message from a bounded local template set.

- Self-reference and user address are inserted into templates.
- Speech style selects the main tone family; personality traits add behavior-specific variations without overriding the selected speech style.
- The first nickname is the actual pet name and may be used in greetings; subsequent nicknames are aliases.
- Catchphrases remain optional offline reaction templates and are not injected into Qwen's normal conversation prompt.
- Response length controls whether a short follow-up sentence is added.
- Emoji level controls whether state-specific emoji are appended.
- A per-avatar/per-state cursor rotates through alternatives before reusing a phrase.

This is **not AI**. It does not interpret arbitrary user input, remember prior conversations or call an external service. It provides differentiated offline behavior until M4 introduces optional AI interaction.

## Automated test coverage

- Schema v1 → v2 migration.
- Preservation of avatar IDs, image paths, image files and selected-avatar settings.
- Default persona creation for legacy profiles.
- Persona persistence across reload, including the built-in avatar.
- Validation of required fields and list limits.
- Primary nickname precedence over the management label.
- Persona-aware tone, self-reference and user-address output.
- Qwen prompt priorities for factual answers and prohibition of action/facial-expression stage directions.
- Message rotation, response length, emoji level and catchphrase selection.

Windows Release CI passed on code commit `30d6ae0139335c09b5bcfc6926fc6df43e55acb9`: Release build with 0 warnings and 0 errors; all 47 automated tests passed ([run #179](https://github.com/wenzhong0606-dev/DesktopPet/actions/runs/45950812579)). Local Windows interaction and migration acceptance remains pending.

## Windows local acceptance checklist

- [ ] Pull the latest \`main\`, restore, build and run tests.
- [ ] Start with an existing schema v1 catalog and confirm it migrates to schema v2.
- [ ] Confirm all imported avatars still appear and their images still load.
- [ ] Confirm the selected avatar is unchanged after migration.
- [ ] Edit the built-in Bichon: set its name to 豆包, self-reference to 豆包, user address to 主人, traits to 活泼/黏人/好奇, and speech style to 可爱.
- [ ] Edit a second avatar with a contrasting persona, e.g. a cool/independent cat.
- [ ] Restart the app and confirm both profiles persist independently.
- [ ] Click each pet repeatedly and confirm the text reflects its own persona and rotates through alternatives.
- [ ] Test response length and emoji settings.
- [ ] Enter four personality traits and confirm validation blocks saving.
- [ ] Confirm M3-A behavior and M1/M2 role management, dragging, snow, lights, settings and tray remain functional.
- [ ] Confirm no AI key, account, microphone or network service is required.

## Out of scope

- AI-generated natural language, conversation memory and arbitrary user text understanding (M4).
- Voice/ASR/TTS.
- 3D models, rigging and skeletal animation.
- Cloud sync, accounts and character marketplace.
