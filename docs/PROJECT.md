# Project Baseline

## Product direction

The product is **AI Desktop Companion**, a Windows desktop companion platform. The repository and executable retain the engineering name \`BichonDesktopPet\` for now; no rename is needed during the MVP phase.

The Bichon Frise is the first built-in avatar. The long-term platform may support different animals, stylized human characters, and user-imported legal assets. Different character types will eventually have different rigs, action capabilities, and behavior profiles.

## Platform baseline

- Target OS: Windows 10/11 x64.
- Technology: .NET 10 / WPF.
- Desktop-pet window: transparent, borderless, 280 × 360 DIP, topmost by default.
- Character presentation: local PNG images with a vector fallback.
- User settings and avatar catalog: local JSON under LocalAppData.
- M0–M3 behavior operation is local and does not require an AI/network service.

## M0 and M1 — accepted baseline

The user confirmed M1 acceptance after the settings-window layout fix. The baseline includes Bichon PNG display, vector fallback, dragging, position persistence, click/gift feedback, snow, decorative lights, system tray, settings, local persistence and diagnostics.

The settings-window layout fix passed Windows CI: [run #65](https://github.com/wenzhong0606-dev/DesktopPet/actions/runs/45942365232). The user subsequently confirmed local acceptance.

## M2 — multiple image avatars (accepted)

**Status: user acceptance passed.**

The implementation includes:

- \`AvatarProfile\`: stable ID, display name, species/type, image path, built-in flag and creation time.
- \`AvatarCatalogService\`: versioned JSON catalog, built-in Bichon seeding, image import, deletion, path validation and local storage.
- \`AvatarManagerWindow\`: import, list, switch, delete and restore-default UI.
- Main-window and system-tray entry points for role management.
- \`PetSettings.ActiveAvatarId\`: remembers the selected role.
- PNG/JPG/JPEG/BMP import, up to 20 MB per image, with extension/signature checks and WPF decode validation.
- Imported images are copied into the current user's LocalAppData folder; repository artwork is not modified.
- The built-in Bichon cannot be deleted.

The user explicitly confirmed M2 acceptance. Windows CI for the M2 baseline passed Release build and all 17 automated tests: [run #97](https://github.com/wenzhong0606-dev/DesktopPet/actions/runs/45944152242).

Only Bichon artwork is bundled. Other roles are added by local import.

## M3-A — lightweight behavior system (accepted)

The first implementation is committed to \`main\`. Windows CI passed on commit `5db5f4dd135f9226665f4defc187c8fe5767ff1d`: Release build with 0 warnings and 0 errors, 30/30 tests passed ([run #124](https://github.com/wenzhong0606-dev/DesktopPet/actions/runs/45946659068)).

The implementation includes:

- \`PetBehaviorEngine\` is platform-independent and deterministic; callers provide timestamps so state transitions can be unit-tested without WPF timers.
- States: Idle, Greeting, Happy, Excited and Sleepy.
- Startup and tray restore trigger Greeting.
- Single clicks trigger Happy; gift interactions and three rapid clicks trigger Excited.
- Transient states return to Idle after their duration. Sixty seconds without interaction transitions to Sleepy.
- Interaction wakes the companion; tray restore triggers Greeting and resets inactivity.
- \`PetBehaviorPresentationResolver\` maps state to a status message and a separate visual-effect identifier, with species-aware wording for dog, cat and rabbit.
- The WPF host renders subtle whole-image breathing, bounce, wiggle and sleepy sway effects.
- The behavior timer is owned by MainWindow and stopped/disposed during application exit.
- Unit tests cover transitions, timing, repeated clicks, wake behavior and species presentation mapping.

**Honest visual limitation:** current role assets are static images. M3 first increment transforms the whole image; it does not animate individual limbs, tail, eyes or mouth. Real frame-sequence or rigged animation is a later increment after suitable assets are supplied.

### M3 first-increment acceptance (passed)

- Windows Release build and all automated tests pass.
- Startup shows a brief greeting, then idle breathing.
- Single click shows a happy response.
- Gift interaction and rapid repeated clicks show an excited response.
- After 60 seconds without interaction the role enters sleepy sway.
- Clicking or restoring from the tray wakes the role.
- Imported dog/cat/rabbit roles display the corresponding status wording.
- M1/M2 functions remain intact: drag, snow, lights, settings, tray, avatar import/switch/delete and persistence.
- No sustained CPU usage or animation continues after application exit.

## M3-B — per-avatar identity and persona system (accepted)

The first vertical slice is committed to \`main\`:

- Add a separate \`PersonaProfile\` attached to each \`AvatarProfile\`, rather than mixing personality fields into the behavior engine.
- Add optional \`Breed\` metadata while keeping \`Species\` as the broader type.
- Upgrade the local avatar catalog from schema v1 to v2. Existing avatar IDs, image paths, image files, names, species and creation timestamps are preserved; existing settings and selected-avatar ID are not rewritten by the migration.
- Migrate missing persona data to safe defaults. Preserve customized built-in Bichon identity/persona on subsequent loads.
- Protect future catalog versions from being overwritten: a catalog created by a newer schema is opened read-only by this version.
- Add a WPF editor reachable from the role manager for built-in and imported avatars.
- Configure pet nicknames, self-reference, user address, up to three free-form personality traits, speech style, response length, emoji level and up to five catchphrases.
- Validate profile lengths/counts and persist changes atomically through the existing catalog save path.
- Connect MainWindow to the active avatar's persona. The local resolver rotates through bounded templates per avatar/state and adjusts tone using speech style, personality traits, nicknames, catchphrases, response length and emoji preference.
- Keep the existing species-only resolver overload as a compatibility fallback.

**Scope boundary:** this is deterministic local template selection, not AI conversation. It does not understand arbitrary user text, retain dialogue memory, or make network requests.

### M3-B acceptance (passed)

- Windows Release build and all automated tests pass.
- Existing v1 catalog migrates to v2 without losing avatar IDs, source images, image paths or selected-avatar settings.
- Built-in and imported avatars can each save and reload their own persona.
- Nicknames, self-reference, user address, traits, speech style, response length, emoji level and catchphrases validate and persist.
- More than three traits or invalid field lengths are rejected without corrupting the previous profile.
- Different avatars produce distinct messages; repeated same-state reactions rotate through alternatives before repeating.
- M3-A behavior states/effects and M1/M2 desktop functions remain intact.
- No AI, voice, 3D, network access or cloud storage is introduced.

## M3-C — companion identity and Qwen provider foundation (accepted)

**Goal:** begin separating the persistent companion individual from the avatar currently used to display it, and prepare the first AI provider without making the desktop pet dependent on network access.

Windows Release CI passed on commit `527edd58cec592d7c3a43775c587d151c26a1c44`: build with 0 warnings and 0 errors; 61/61 automated tests passed ([run #224](https://github.com/wenzhong0606-dev/DesktopPet/actions/runs/45960178698)). The user confirmed M3 acceptance on 2026-11-27.

Implementation:

- Add \`CompanionIdentity\` with a stable ID, current avatar link, display name, initial-persona snapshot and an evolving-personality placeholder.
- Store companion identities in \`%LOCALAPPDATA%\\BichonDesktopPet\\companions.db\`, separate from the existing avatar catalog and settings JSON.
- Create a local interaction journal for pet clicks, gift interactions and tray wake events. Raw user conversation text is not recorded.
- Archive companion identity/history when an avatar is deleted instead of automatically deleting its accumulated history.
- Add the provider-neutral \`IAiChatProvider\` contract and a non-streaming Qwen Chat Completions implementation.
- Default provider configuration is Qwen, model \`qwen-plus\`, endpoint \`https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions\`.
- Read the API key from \`Qwen__ApiKey\` or the optional local file \`%LOCALAPPDATA%\\BichonDesktopPet\\appsettings.Local.json\`; environment variables override file values.
- M3-C initialized the provider without invoking it. M4-A now calls it only after the user explicitly sends a message; startup still sends no AI request, and no API key is required for existing offline features.

**Important limitations:** this is not yet a chat experience. \`EvolvingPersonalityJson\` remains an empty placeholder; interaction events are not yet converted into memories; no semantic retrieval, automatic memory extraction or autonomous personality growth is implemented.

### M3-C acceptance (passed)

- Windows Release build and all 61 automated tests pass with 0 warnings and 0 errors.
- Existing settings, avatar catalog, avatar IDs and imported images remain untouched.
- On first run, each existing avatar receives one stable companion ID in the separate SQLite database.
- Restarting the app preserves companion IDs and local interaction events.
- Deleting an imported avatar archives its companion record and keeps the interaction journal.
- The app starts and all offline features work when no Qwen API key is configured.
- The exact Qwen defaults are \`qwen-plus\` and the configured DashScope compatible endpoint.
- The API key is loaded locally and never committed or written to logs.
- No network request is sent during startup.

### Next increment

M4-A will add an explicit text conversation UI and a compact context builder. M4-B will add memory formation/retrieval and gradual personality development. These features must use the stable companion ID and remain independent of the Qwen provider.

## Local storage

- Settings: \`%LOCALAPPDATA%\\BichonDesktopPet\\settings.json\`
- Avatar catalog: \`%LOCALAPPDATA%\\BichonDesktopPet\\avatars\\catalog.json\`
- Imported image: \`%LOCALAPPDATA%\\BichonDesktopPet\\avatars\\{avatar-id}\\avatar.{extension}\`

## Milestones

- M0: image-based desktop pet display — accepted.
- M1: reliable daily-use image desktop pet — user acceptance passed.
- M2: multiple image avatars and local avatar management — user acceptance passed.
- M3: local behavior, per-avatar persona profiles, persistent companion identity, local interaction journal and Qwen provider foundation — M3-C accepted by the user on 2026-11-27.
- M4-A: explicit AI text interaction and compact context construction using Qwen `qwen-plus` by default.
- M4-B: local memory formation/retrieval and gradual, evidence-based personality development.
- M5: push-to-talk voice interaction and speech output.
- M6: single-character real-time 3D technical validation.
- M7: user-imported GLB/glTF static avatar support.
- M8: rig profiles and character-specific animation expansion.
- M9: optional character sharing/ecosystem, only after earlier stages are validated.

See [PRODUCT_DEFINITION_AND_MVP_ROADMAP_V1.md](PRODUCT_DEFINITION_AND_MVP_ROADMAP_V1.md), [M2_AVATAR_MANAGEMENT.md](M2_AVATAR_MANAGEMENT.md), [M3_BEHAVIOR_SYSTEM.md](M3_BEHAVIOR_SYSTEM.md) and [M3B_PERSONA_SYSTEM.md](M3B_PERSONA_SYSTEM.md), [M3C_COMPANION_FOUNDATION.md](M3C_COMPANION_FOUNDATION.md) and [QWEN_CONFIGURATION.md](QWEN_CONFIGURATION.md).


## 12. M4-A — AI 认知与文字交流（进行中）

M3 已于 2026-11-27 经用户确认全部验收通过并正式关闭。当前研发阶段为 M4-A。

M4-A 首个垂直切片（实现中）：
- 从桌宠右键菜单和系统托盘菜单显式打开独立聊天窗口。
- 复用现有 `IAiChatProvider` / Qwen `qwen-plus`，不在启动时发送请求。
- 将当前角色 PersonaProfile、伙伴身份显示名和当前行为状态构建为 system context。
- 每次请求仅附带当前聊天窗口内最近 12 条 user/assistant 消息；单条上下文消息限制为 2,000 字符。
- 发送前明确提示：用户消息、角色人格设定、当前行为状态和最近对话会发送至 Qwen 云端处理；提醒用户不要输入敏感信息。
- 聊天会话和 user/assistant 消息保存在本机现有 `companions.db` 的独立 `chat_sessions` / `chat_messages` 表中，不写入交互事件日志，也不做长期记忆提取。
- 每个伙伴身份独立保存会话；再次打开聊天窗口会恢复该角色最近一次会话，并支持切换、删除会话和清空当前角色全部聊天记录。
- 历史消息采用 SQLite keyset pagination：首次只加载最近 30 条，点击“加载更早消息”时再查询上一页；单页上限 100 条，消息列表启用 WPF 虚拟化。
- 用户消息先写入本地数据库再发送给 Qwen；AI 回复收到后写入本地数据库。关闭窗口或重启应用不会删除已保存记录。
- 未配置 API Key 时，聊天入口明确提示配置要求并禁用发送；所有原有离线桌宠功能继续可用。
- 请求超时、取消、网络错误和 Provider 错误均提供用户可见反馈。

此切片仍不包含流式输出、记忆检索、自动记忆提取、人格成长、语音或 AI 工具调用。聊天记录仅保存在本机 SQLite，不会随历史记录功能自动上传；用户主动发送消息时仍会按 Qwen 请求需要发送当前消息和有限上下文。需经 Windows CI 与用户本机验收后，才可标记 M4-A 后续增量完成。
