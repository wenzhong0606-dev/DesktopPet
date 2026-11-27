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
- When the user sends a chat message in M4-A, the message, active persona settings, current behavior state and latest 12 conversation turns are transmitted to the configured Qwen endpoint for processing. Do not enter sensitive information.
- Requests use HTTPS.
- If the key is absent, AI is marked as not configured; the desktop pet still runs offline.
- M3-C introduced the provider without invoking it. M4-A now provides an explicit chat window and sends requests only after the user submits a message. Application startup never sends an AI request.
