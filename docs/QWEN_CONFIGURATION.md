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
- Requests use HTTPS.
- If the key is absent, AI is marked as not configured; the desktop pet still runs offline.
- M3-C only prepares the provider. It does not send a request on startup or provide the chat UI yet. M4-A will connect explicit user conversations to the provider.
