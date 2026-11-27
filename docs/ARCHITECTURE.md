# Architecture

## Solution layout

- src/BichonDesktopPet.App: WPF executable, views, window lifecycle, platform services, tray icon, audio, and particle rendering.
- src/BichonDesktopPet.Core: platform-independent models and settings contracts.
- tests/BichonDesktopPet.Tests: unit tests for core defaults and behavior.
- assets: source and production character assets, animation frames, audio, and icons.
- docs: product, architecture, and visual specifications.

## Runtime flow

1. App creates the pet window and system tray icon.
2. MainWindow loads local settings and the versioned avatar catalog, then resolves the selected avatar ID.
3. AvatarCatalogService resolves built-in assets from the packaged Assets folder and imported assets from LocalAppData. If the selected custom avatar is missing, the app falls back to the built-in Bichon.
4. MainWindow decodes the selected image at a bounded display width. If the built-in image is missing or invalid, the temporary vector fallback remains visible and the status label reports the asset state.
5. SnowAnimationService owns a single render-priority dispatcher timer and a bounded particle collection.
6. SettingsService reads and writes JSON defensively; malformed settings never block startup.
7. AvatarCatalogService imports PNG/JPG/JPEG/BMP files into the user's LocalAppData folder, validates extension/signature, protects the built-in Bichon and persists a versioned JSON catalog.
8. AudioService looks for an optional authorized audio file and does nothing when it is absent.
9. TrayIconService provides show, hide, settings, avatar management, snow, lights, music, and exit actions.

## Engineering constraints

- Keep the window lightweight and avoid one timer per particle.
- Keep all animation updates on the WPF dispatcher.
- Bound particle counts and release timers/media resources on exit.
- Keep user settings, companion identities and chat history local; do not add telemetry. M4-A sends a request to Qwen only after the user explicitly submits a message. Local history browsing never sends network requests.
- Keep original artwork separate from optimized runtime assets.
- Do not commit generated binaries, build outputs, or user-specific settings.
- Avoid adding third-party runtime dependencies unless a concrete requirement justifies it.

## Character asset loading

The app uses a content file copied beside the executable. BitmapImage is initialized with BitmapCacheOption.OnLoad, then frozen so the source file can be released and the image can be used safely by WPF rendering.

The expected repository path is assets/characters/bichon/base/bichon-idle.png. The project file's existing content wildcard copies the PNG to Assets/characters/bichon/base/ in the build output.

## Build and test

Run on Windows with the .NET 10 SDK:

~~~powershell
dotnet restore BichonDesktopPet.sln
dotnet build BichonDesktopPet.sln -c Release
dotnet test BichonDesktopPet.sln -c Release
~~~

The WPF application is Windows-targeted. EnableWindowsTargeting is set so SDK-based builds can restore Windows targeting packs from non-Windows build agents, but runtime visual validation must be performed on Windows.


## Local avatar catalog (M2)

- \`AvatarProfile\` is a platform-independent model with stable ID, display name, species/type metadata, relative image path, built-in flag and creation timestamp.
- \`AvatarCatalogService\` owns \`%LOCALAPPDATA%\\BichonDesktopPet\\avatars\\catalog.json\` and imported image folders.
- Built-in Bichon art resolves from the packaged \`Assets/characters/bichon/base\` directory. Imported art resolves only inside the local avatars directory.
- User images are copied into LocalAppData; the original source file is never modified.
- Supported file types are PNG, JPG, JPEG and BMP, limited to 20 MB per image. The UI performs WPF decode validation before import.
- \`PetSettings.ActiveAvatarId\` stores the current role. If the saved custom role no longer exists, startup restores the built-in Bichon.
- The manager UI is a WPF view; catalog persistence and file validation remain in Core so they can be unit-tested without WPF.
- M2 does not add 3D, animation retargeting, AI, voice, network sync or a marketplace.


## Behavior system (M3)

- \`PetBehaviorEngine\` lives in Core and is independent of WPF. It accepts caller-supplied timestamps and deterministically transitions among Idle, Greeting, Happy, Excited and Sleepy.
- Startup and tray restore trigger Greeting; click triggers Happy; gift interaction or three rapid clicks trigger Excited; 60 seconds of inactivity triggers Sleepy.
- Greeting, Happy and Excited have bounded durations and return to Idle unless the inactivity threshold has already elapsed.
- \`PetBehaviorPresentationResolver\` maps the state to a short message and a \`PetVisualEffect\`. Species-specific wording is separate from state transitions.
- \`PetBehaviorAnimationService\` is the WPF adapter. It maps effects to subtle transforms of the whole static image. It does not claim skeletal or per-limb animation.
- MainWindow owns one low-frequency DispatcherTimer for state advancement. The timer and animation service are stopped/disposed on application exit.
- Keep behavior rules in Core and rendering in App. Future animated frame sequences or 3D rigs can replace the presenter without rewriting the state machine.
- M3 first increment is offline and does not call AI, network services, microphone or speech APIs.


## Per-avatar persona and message presentation (M3-B)

- \`AvatarProfile\` owns a separate \`PersonaProfile\`; appearance metadata (species/breed/image path), identity/personality, behavior state and rendering remain separate concerns.
- \`PersonaProfile\` stores nicknames, self-reference, user address, up to three personality traits, speech style, response length, emoji level and up to five catchphrases.
- \`AvatarCatalogService\` owns schema v2. On v1 load it adds safe persona defaults and writes v2 while preserving valid avatar IDs, image paths, creation times and existing files. \`PetSettings.ActiveAvatarId\` is stored separately and is not modified by the catalog migration.
- A catalog with a schema version newer than the app's supported version is opened read-only to avoid accidental downgrade writes.
- \`PersonaProfileValidator\` validates editor input and normalizes persisted data. The WPF \`PersonaEditorWindow\` edits both built-in and imported profiles through \`AvatarCatalogService.UpdateProfile\`.
- \`PetBehaviorPresentationResolver\` accepts the active \`AvatarProfile\`, selects bounded local templates using state/species/persona, and rotates templates per avatar/state. Speech style is independent from personality traits; common trait tags add variations, while nicknames/catchphrases are available across speech styles. It does not call an AI provider, interpret free-form user text or store conversation memory.
- The older species-only resolver overload remains as a fallback for callers that do not have a full avatar profile.
- Future AI integration should consume \`PersonaProfile\` as context, but must remain behind a separate provider adapter and user opt-in; persona persistence itself has no network dependency.


## Companion identity and local interaction journal (M3-C)

- \`AvatarProfile\` remains the current visual/avatar catalog record. \`CompanionIdentity\` is a separate persistent individual with its own stable ID.
- \`CompanionIdentityStore\` uses SQLite at \`%LOCALAPPDATA%\\BichonDesktopPet\\companions.db\`; it does not rewrite \`settings.json\` or \`avatars/catalog.json\`.
- On startup, existing avatars are synchronized to companion records. The identity ID and creation timestamp remain stable when display name or initial persona is refreshed.
- The initial persona snapshot is stored separately from the evolving-personality placeholder. M3-C does not yet calculate personality growth.
- The local interaction journal records only bounded event metadata for pet clicks, gift interactions and tray wake. It does not record free-form user text.
- Deleting an avatar archives the linked companion record and retains its event history. Reattachment/archive management UI is a later increment.

## AI provider foundation (M3-C)

- \`IAiChatProvider\` is the provider-neutral text generation boundary.
- \`QwenChatCompletionProvider\` calls the DashScope OpenAI-compatible Chat Completions endpoint using model \`qwen-plus\`.
- \`QwenOptionsLoader\` reads the local settings file under LocalAppData and lets \`Qwen__ApiKey\`, \`Qwen__Endpoint\` and \`Qwen__Model\` environment variables override file values.
- The application creates the provider at startup and exposes it as \`App.AiChatProvider\`. M4-A's explicit chat window calls it only after a user sends a message; startup never sends a network request.
- Missing or invalid local configuration does not prevent the offline desktop pet from starting. API keys are not logged.
- Version one supports only system/user/assistant messages, non-streaming responses and usage extraction. Conversation persistence and pagination are implemented by the separate local ChatHistoryStore, not by the provider adapter. Tool calls, automatic retries, semantic memory retrieval and streaming remain out of scope.
- Qwen is the default provider, but companion identity, local storage and behavior code depend on the provider-neutral contract.



## AI text conversation (M4-A, first vertical slice)

- The explicit chat window is opened by the user from the pet context menu or tray menu. App startup never sends an AI request.
- `ChatWindow` calls the existing provider-neutral `IAiChatProvider`; the first configured provider remains Qwen `qwen-plus`.
- `ChatContextBuilder` builds a system message from the active avatar persona, display name and current behavior state. It appends only the latest 12 user/assistant messages and bounds each message to 2,000 characters.
- Before sending, the UI discloses that user text, active persona settings, current behavior state and recent turns are sent to Qwen cloud for processing, and warns against entering sensitive information.
- `ChatHistoryStore` persists sessions and user/assistant messages locally in the existing `companions.db`, keyed by stable companion ID. It does not write message text to the interaction journal.
- Message history uses keyset pagination by the indexed auto-increment message ID. The initial view loads the latest 30 messages; older pages are queried only when requested, with a maximum page size of 100.
- The WPF message list uses a recycling `VirtualizingStackPanel`; only visible item containers are realized. The in-memory AI context remains bounded to the latest 12 messages and is independent of how many historical pages are viewed.
- The chat window uses a lightweight modern IM layout with a dedicated identity header, session toolbar, aligned role-specific bubbles and a separate composer. Existing pagination controls, keyboard submission and list virtualization remain in place.
- `ChatWelcomeMessageBuilder` generates the empty-session greeting locally from the active avatar's display name and normalized `PersonaProfile` (self-reference, user address, speech style, recognized traits, nickname, catchphrase, response length and emoji level). The greeting is a transient UI-only placeholder with message ID 0; it is cleared before the first user message is appended, never saved to SQLite and never included in `ChatContextBuilder` history.
- The chat UI restores the latest session for the active companion and supports session switching, deleting one session and clearing all sessions for that companion. Foreign-key cascade removes messages when a session is deleted.
- User messages are committed locally before an outbound Qwen request; successful assistant responses are persisted after receipt. Closing/reopening the window does not remove saved history.
- When Qwen is not configured, the chat UI explains the local configuration requirement and disables sending. Existing desktop-pet features remain offline and available.
- Requests are asynchronous, single-flight and bounded by a 50-second cancellation timeout. The UI handles cancellation, provider failures and network failures without logging conversation content.
- M4-A first slice does not implement streaming, semantic memory retrieval, automatic memory extraction, personality growth, voice, tool calls or arbitrary system actions.
