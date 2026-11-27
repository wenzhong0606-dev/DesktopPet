# M3-C — Companion Identity and AI Foundation

## Goal

Start treating each desktop pet as a persistent individual rather than treating its avatar image as the individual. Prepare the first AI provider without making the desktop app depend on network access.

This is a foundation milestone. It does not yet deliver a chat UI, AI-generated conversation, automatic memory extraction or personality growth.

## Companion identity

The new \`CompanionIdentity\` is stored separately from \`AvatarProfile\`.

- \`CompanionIdentity.Id\` is a stable ID generated once and persisted in SQLite.
- \`AvatarId\` identifies the avatar currently used to present the companion.
- \`DisplayName\` and \`CorePersonalityJson\` are refreshed from the existing avatar profile.
- \`EvolvingPersonalityJson\` is reserved for later gradual development and remains an empty JSON object in M3-C.
- Re-importing or renaming an avatar does not silently rewrite an existing companion ID.
- Removing an avatar archives its companion record. Interaction history is retained rather than being deleted with the image.

Current limitation: the user interface does not yet offer an archive browser or a way to attach a replacement avatar to an archived companion. Those belong to a later companion-management increment.

## Local interaction journal

Database: \`%LOCALAPPDATA%\\BichonDesktopPet\\companions.db\`

M3-C creates two tables:

- \`companions\`: stable identity, current avatar link, core-personality snapshot, evolving-personality placeholder and timestamps.
- \`interaction_events\`: bounded local event records linked to a companion.

The WPF shell currently records only:
- \`pet_clicked\`
- \`gift_opened\`
- \`woke_from_tray\`

The event journal does not record free-form user text, screenshots, files, audio or other personal content. It is local-only. M3-C does not infer memories from these events.

## Qwen provider foundation

The provider contract is \`IAiChatProvider\`. The first implementation is \`QwenChatCompletionProvider\`.

Defaults:
- Provider: Qwen
- Model: \`qwen-plus\`
- Endpoint: \`https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions\`
- Protocol: non-streaming OpenAI-compatible Chat Completions
- Supported message roles: \`system\`, \`user\`, \`assistant\`

The client validates request roles and token bounds, supports cancellation, returns usage fields when present, redacts the configured API key from provider error details, and does not execute tools or arbitrary functions.

The WPF application creates the provider at startup and exposes it through \`App.AiChatProvider\`. If no key is configured, \`IsConfigured\` is false and all existing offline pet functions continue to work.

**Important:** M3-C does not send an AI request at startup and does not yet connect a chat UI. The provider is ready for M4-A, but no user conversation is transmitted in this milestone.

## Local Qwen configuration

The application reads:

\`%LOCALAPPDATA%\\BichonDesktopPet\\appsettings.Local.json\`

Example:

~~~json
{
  "Qwen": {
    "ApiKey": "your-api-key",
    "Endpoint": "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions",
    "Model": "qwen-plus"
  }
}
~~~

Environment variables override local-file values:
- \`Qwen__ApiKey\`
- \`Qwen__Endpoint\`
- \`Qwen__Model\`

The local file is ignored by Git. Never commit an API key or paste it into source code, issues or chat. See [QWEN_CONFIGURATION.md](QWEN_CONFIGURATION.md).

## Windows CI result

Windows Release CI passed on commit `527edd58cec592d7c3a43775c587d151c26a1c44`: 0 warnings, 0 errors and 61/61 automated tests ([run #224](https://github.com/wenzhong0606-dev/DesktopPet/actions/runs/45960178698)). Local Windows interaction acceptance remains pending.

## Automated coverage

- Stable identity creation and lookup by avatar ID.
- Identity ID and creation time remain stable when the avatar profile is refreshed.
- Core-personality snapshot updates without replacing the identity.
- Interaction event persistence and validation.
- Removed avatars are archived while event history is retained.
- Qwen defaults, local-file loading and environment-variable precedence.
- Missing API key prevents network requests.
- OpenAI-compatible request construction, response parsing, usage extraction and error handling.
- Unsupported tool roles are rejected before a request is sent.

## Not included

- AI chat window or automatic conversation.
- Long-term semantic memory extraction or retrieval.
- Autonomous personality growth.
- AI-generated actions or tool/function calling.
- Voice, 3D rendering, rigging or animation generation.
- Cloud storage, account sync or telemetry.
