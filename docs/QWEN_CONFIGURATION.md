# Local Qwen Configuration

The first AI provider is the exact model identifier \`qwen-plus\`, using the DashScope OpenAI-compatible Chat Completions endpoint.

## Default values

- Model: \`qwen-plus\`
- Endpoint: \`https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions\`

The model and endpoint are configurable. The API key is never stored in Git.

## Option A — local settings file (recommended for development)

Create this file under the current Windows user's LocalAppData directory:

\`%LOCALAPPDATA%\\BichonDesktopPet\\appsettings.Local.json\`

PowerShell:

~~~powershell
$directory = Join-Path $env:LOCALAPPDATA "BichonDesktopPet"
New-Item -ItemType Directory -Force -Path $directory | Out-Null
notepad (Join-Path $directory "appsettings.Local.json")
~~~

Paste the following JSON and replace the placeholder with your own key:

~~~json
{
  "Qwen": {
    "ApiKey": "replace-with-your-api-key",
    "Endpoint": "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions",
    "Model": "qwen-plus"
  }
}
~~~

Save the file and restart the desktop pet. The application reads this file at startup.

## Option B — environment variables

The application also reads these variables:

- \`Qwen__ApiKey\`
- \`Qwen__Endpoint\`
- \`Qwen__Model\`

For the current PowerShell session:

~~~powershell
$env:Qwen__ApiKey = "replace-with-your-api-key"
~~~

Environment variables override the corresponding values in the local settings file. Restart the app after changing a persistent user environment variable.

## Security and behavior

- Do not add \`appsettings.Local.json\` to Git.
- Do not commit or share your API key.
- The repository ignores local settings files with the \`.local.json\` / \`.Local.json\` suffix and \`appsettings.Local.json\` specifically.
- The key is held in memory for the provider lifetime and is not written to application logs.
- Chat sessions and message content are stored locally in the SQLite database at %LOCALAPPDATA%\\BichonDesktopPet\\companions.db; browsing or paging through saved history does not send network requests.
- The SQLite history is local application data and is not encrypted by this feature. Protect access to your Windows account/device and use the in-app delete/clear controls when needed.
- When the user sends a chat message in M4-A, the message, primary pet nickname, active persona settings and latest 12 conversation turns are transmitted to the configured Qwen endpoint for processing. Internal animation/behavior state and role-management labels are excluded. Do not enter sensitive information.
- The prompt prioritizes accurate, relevant answers over role-play and forbids action/facial-expression stage directions. Common bracketed action asides are also removed from the returned text before it is displayed or persisted; ordinary explanatory parentheses are preserved.
- Requests use HTTPS.
- If the key is absent, ordinary AI chat is marked as not configured; current-time/date/weekday questions are still answered locally without a key or cloud request.
- M3-C introduced the provider without invoking it. M4-A now provides an explicit chat window and sends Qwen requests only after the user submits a message. Current-time/date/weekday questions are handled locally. Application startup never sends a Qwen request. Separately, city geocoding and weather requests are sent only after the user enables weather reminders; see `LOCAL_ENVIRONMENT_AND_CARE.md` for provider/privacy details.


### M4-B memory privacy

Long-term memories are stored separately in the local companion database. If the user explicitly enables memory retrieval for a companion, a cloud chat request may include relevant active entries selected by topic, finite aliases, category and limited intent hints, within a 2,400-byte UTF-8 JSON budget and a 12-entry safety ceiling. Archived and expired representative entries are excluded; importance never bypasses relevance. The budget is not an exact Qwen token count. This is disabled by default. Turning it off stops subsequent retrieval but cannot retract prior requests or erase ordinary chat history. Memory management itself never calls Qwen. A separate opt-in enables local automatic extraction of explicit ordinary preferences from new user messages; it does not call Qwen, does not scan prior history and does not enable cloud retrieval. Existing chat-history transmission is unchanged.

Automatic same-topic memories can now be updated by newer explicit user statements; user-confirmed manual entries are not overwritten. Automatic extraction still runs locally and never triggers a separate Qwen call.

The local automatic memory scope includes finite ordinary milestones, explicit communication preferences and verified conversation topics. Conversation memories establish that a request received a persisted reply, not that a real-world task was completed. Source labels distinguish user statements from observed exchanges. Related entries may be transmitted only with existing retrieval authorization; no separate extraction request is made.

Growth adaptation has a separate default-off consent. When enabled, normal chat requests may include a small fixed-language expression hint derived from repeated explicit user feedback. Raw growth evidence, source IDs, counters and growth history are not transmitted as growth context. Pausing suppresses the derived hints; resetting growth does not erase earlier requests or chat history. Memory retrieval consent is independent.


Optional cloud-assisted extraction: separate default-off setting under companion memory; requires automatic memory. At most one additional JSON Object request for a narrowly gated current message (160 characters, 180 output tokens, 5-second cancellation deadline), potentially billable. No memory database/history is sent for this additional request. Local validation accepts only exact, independently recognized clauses. Provider/model incompatibility or failure falls back to local processing. Ordinary requests retain their original text format. See https://www.alibabacloud.com/help/en/model-studio/qwen-structured-output .
