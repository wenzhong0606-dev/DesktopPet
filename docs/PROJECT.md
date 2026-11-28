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
- No Qwen request is sent during startup. Optional location/weather requests occur only after the user explicitly enables weather reminders.

### Next increment

M4-A now includes the explicit text conversation UI, bounded context builder and local paginated chat history. Remaining M4-A work is driven by Windows user acceptance and conversation UX hardening. M4-B will add memory formation/retrieval and gradual personality development using the stable companion ID, independently of the Qwen provider.

## Local storage

- Settings: \`%LOCALAPPDATA%\\BichonDesktopPet\\settings.json\`
- Companion identities, interaction journal and paginated chat history: %LOCALAPPDATA%\\BichonDesktopPet\\companions.db
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

M4-A 首个垂直切片（已实现；Windows CI 通过，待用户本机验收）：
- 从桌宠右键菜单和系统托盘菜单显式打开独立聊天窗口。
- 复用现有 `IAiChatProvider` / Qwen `qwen-plus`，不在启动时发送请求。
- 将宠物首个昵称和当前 PersonaProfile 构建为 system context；不把角色管理名称或内部行为/动画状态发送给模型。
- 每次请求仅附带当前聊天窗口内最近 12 条 user/assistant 消息；单条上下文消息限制为 2,000 字符。
- 发送前明确提示：用户消息、宠物昵称/人格设定和最近对话会发送至 Qwen 云端处理；内部动画状态不进入模型上下文；提醒用户不要输入敏感信息。
- 聊天会话和 user/assistant 消息保存在本机现有 `companions.db` 的独立 `chat_sessions` / `chat_messages` 表中，不写入交互事件日志，也不做长期记忆提取。
- 每个伙伴身份独立保存会话；再次打开聊天窗口会恢复该角色最近一次会话，并支持切换、删除会话和清空当前角色全部聊天记录。
- 历史消息采用 SQLite keyset pagination：首次只加载最近 30 条，点击“加载更早消息”时再查询上一页；单页上限 100 条，消息列表启用 WPF 虚拟化。
- 用户消息先写入本地数据库再发送给 Qwen；AI 回复收到后写入本地数据库。关闭窗口或重启应用不会删除已保存记录。
- 未配置 API Key 时，当前日期/时间/星期问题仍可由本机时钟离线回答；其他 AI 对话明确提示需要配置密钥。
- 请求超时、取消、网络错误和 Provider 错误均提供用户可见反馈。
- 聊天窗口采用克制的桌面对话工作区视觉：中性色背景、细分隔线、充足留白和轻量消息底色；保留键盘发送与历史分页虚拟化。
- `AvatarProfile.Name` 仅为角色管理标签；`PersonaProfile.Nicknames` 第一项是宠物常用名字，后续项是别名。聊天标题、欢迎语和 AI 身份上下文均使用首个昵称，不读取管理标签作为宠物名字。
- 空会话欢迎语由 Core 的 `ChatWelcomeMessageBuilder` 本地生成，读取宠物常用昵称、自称、用户称呼、语言风格和性格；不调用 Qwen、不写入 SQLite，也不加入后续 AI 上下文。

此切片仍不包含流式输出、记忆检索、自动记忆提取、人格成长、语音或 AI 工具调用。聊天记录仅保存在本机 SQLite，不会随历史记录功能自动上传；用户主动发送消息时仍会按 Qwen 请求需要发送当前消息和有限上下文。Windows CI 已通过（114/114 测试，0 警告、0 错误）；仍需用户本机验收后才能关闭本次增量。


### 2026-11-27 — 宠物昵称语义、对话准确性与窗口视觉修订

- `AvatarProfile.Name` 仅为角色管理标签；`PersonaProfile.Nicknames` 第一项是宠物常用名字，后续项是别名。聊天标题、欢迎语、行为问候和 AI 身份上下文均使用首个昵称，不读取管理标签作为宠物名字。
- 聊天窗口采用克制的桌面对话工作区视觉：中性色背景、细分隔线、充足留白和轻量消息底色，不使用传统大圆角气泡或嵌套卡片；保留键盘发送与历史分页虚拟化。
- 角色管理和个性编辑窗口使用同一低装饰度视觉规范，并明确区分管理名称与宠物昵称。
- AI system prompt 将事实准确和直接回答置于角色表达之前，排除内部动画状态与口头禅，并明确禁止动作、神态和表情旁白；回复入库前由 `ChatReplySanitizer` 清理常见括号/星号动作旁白，保留正常解释性括号。


## 13. 本机环境感知与主动关怀（M4-A 增量，已实现待本机验收）

- `SystemClockService` 读取 Windows 本机日期、时间和时区；`LocalTimeQueryHandler` 对明确的当前时间、日期和星期问题直接本地回答，不调用 Qwen、不消耗 Token。普通 AI 对话的上下文也包含本机时间和时区。
- `WindowsActivityMonitor` 只读取 Windows `GetLastInputInfo` 的最近输入空闲时长；不采集按键内容、鼠标坐标、应用名称、窗口标题、网页或文件。该能力默认开启，可在设置中关闭。
- `ActivityRhythmEngine` 不要求填写固定作息：空闲达到 5 分钟即结束连续活跃段；连续活跃约 60 分钟提醒休息，之后至少间隔 90 分钟才可能再次提醒。当前版本是本地会话级规则，不推断睡眠时间，也不建立跨日行为画像。
- IP 自动城市定位和天气提醒默认关闭。天气提醒启用后，用户可手动设置城市（优先级最高），或单独授权 IP 自动定位。
- `NetworkPrivacyInspector` 在本地检查系统代理和已启用的隧道/VPN 网络接口；存在疑似代理/VPN 时不发起 IP 定位。否则调用 IPLogs 的当前出口检测，只有 clean 且分数低于 0.30、`is_vpn` 明确为 false 且没有任何代理/Tor/Relay/Hosting/Satellite 正向标记时才接受城市/国家，再由 Open-Meteo Geocoding 解析城市坐标；自动 IP 城市的时区还必须与 Windows 本机时区相差小于 90 分钟。
- 被识别为匿名网络或可信度不足时，不使用 VPN/代理出口城市；保留上次可信城市（若有），否则要求用户手动指定城市。不会尝试反查 VPN 隧道内的原始公网 IP。
- 仅缓存城市、国家、城市级经纬度、来源和更新时间；不保存原始公网 IP。手动城市覆盖自动 IP 城市。IPLogs 声明其访问日志保留 30 天；启用前在设置中明确告知用户。
- `OpenMeteoWeatherService` 使用 Open-Meteo 地理编码和逐小时预报；`WeatherReminderPolicy` 仅根据真实预报触发降温/添衣、降雨/带伞和高温/补水提示。按类型持久化 6 小时冷却，天气刷新周期为 1 小时；服务失败或数据过期时不生成天气提醒。关闭天气关怀或更改关注城市时会取消在途请求。
- 天气服务只在用户启用天气提醒后请求；城市级手动位置和 IP 位置都不会发送给 Qwen。Open-Meteo 免费 API 的使用需遵守其非商业许可和署名要求；商业发行前必须重新核实授权。
- 当前版本未接入 Windows 精确地理位置 API；IP 城市是近似值，VPN/代理检测也不能保证识别所有住宅代理。用户始终可以手动指定城市。

验收重点：本机时间离线回答；持续活跃/空闲状态重置与提醒冷却；IPLogs clean/VPN/proxy/relay 等结果的 fail-closed 处理；VPN 环境不采纳出口城市；手动城市覆盖；天气 API 不可用或数据过期时不误提醒；天气提醒重启后仍遵守冷却。


## 14. M4-B — 本地长期记忆与自然成长（开发中）

2026-11-28：用户决定延后 M4-A 本机验收，先推进 M4-B。M4-A 仍待验收。

M4-B1 基线 CI 已通过。本轮 M4-B1 修订 + M4-B2 首个增量：解除固定总量上限，按页读取和数据库索引检索；独立授权的本地自动记忆提取，保存明确用户偏好的分类、来源与证据等级；同主题不自动覆盖。当前仅覆盖有限白名单的明确偏好陈述，最多四条相关记忆进入 Qwen 上下文。更广泛的自然语言提取、冲突管理、语义检索、经历整理和人格成长仍待推进。详见 [M4B_MEMORY_SYSTEM.md](M4B_MEMORY_SYSTEM.md)。


M4-B2 第二增量 / M4-B3 基础冲突管理：上一增量 Windows CI 137/137 通过。扩展本地提取语法、普通偏好词库和色彩分类；较新明确陈述更新同主题自动记忆，手动确认保持优先。持久化每角色消息处理游标，防止重复回放和旧记忆复活。仍不使用额外云端提取请求，复杂自由文本理解、跨主题语义合并、记忆有效期、归档及动态 Token 预算尚待后续。M4-A 和 M4-B 本机验收继续延后。
