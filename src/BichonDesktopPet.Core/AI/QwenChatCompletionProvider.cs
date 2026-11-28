using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BichonDesktopPet.Core.AI;

/// <summary>
/// Minimal non-streaming Qwen Chat Completions client using the OpenAI-compatible
/// DashScope endpoint. It has no retry loop, tool execution or hidden memory behavior.
/// </summary>
public sealed class QwenChatCompletionProvider : IAiChatProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> SupportedRoles = new(StringComparer.Ordinal)
    {
        "system",
        "user",
        "assistant"
    };

    private readonly HttpClient _httpClient;
    private readonly QwenOptions _options;

    public QwenChatCompletionProvider(HttpClient httpClient, QwenOptions options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string ProviderName => "Qwen";

    public string Model => _options.Model;

    public bool IsConfigured => _options.IsConfigured;

    public async Task<AiChatResult> CompleteAsync(
        AiChatRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!IsConfigured)
        {
            throw new AiProviderNotConfiguredException(ProviderName);
        }

        if (!Uri.TryCreate(_options.Endpoint, UriKind.Absolute, out var endpoint) ||
            endpoint.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("The Qwen endpoint must be an absolute HTTPS URL.");
        }

        if (string.IsNullOrWhiteSpace(_options.Model))
        {
            throw new InvalidOperationException("The Qwen model name is required.");
        }

        if (request.Messages is null || request.Messages.Count == 0)
        {
            throw new ArgumentException("At least one chat message is required.", nameof(request));
        }

        if (request.MaxTokens is < 1 or > 8192)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "MaxTokens must be between 1 and 8192.");
        }

        if (request.Temperature is < 0 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Temperature must be between 0 and 2.");
        }

        foreach (var message in request.Messages)
        {
            if (message is null ||
                !SupportedRoles.Contains(message.Role) ||
                string.IsNullOrWhiteSpace(message.Content))
            {
                throw new ArgumentException(
                    "Messages must have a supported role and non-empty content.",
                    nameof(request));
            }
        }

        var payload = new Dictionary<string, object?>
        {
            ["model"] = _options.Model,
            ["messages"] = request.Messages.Select(message => new
            {
                role = message.Role,
                content = message.Content
            }).ToArray(),
            ["max_tokens"] = request.MaxTokens
        };

        if (request.JsonObjectResponse)
            payload["response_format"] = new { type = "json_object" };

        if (request.Temperature.HasValue)
        {
            payload["temperature"] = request.Temperature.Value;
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(payload, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw CreateProviderError(response.StatusCode, responseBody, _options.ApiKey);
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;

            if (!root.TryGetProperty("choices", out var choices) ||
                choices.ValueKind != JsonValueKind.Array ||
                choices.GetArrayLength() == 0 ||
                !choices[0].TryGetProperty("message", out var messageElement) ||
                !messageElement.TryGetProperty("content", out var contentElement) ||
                contentElement.ValueKind != JsonValueKind.String)
            {
                throw new AiProviderException("Qwen returned a response without assistant message content.");
            }

            var content = contentElement.GetString();
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new AiProviderException("Qwen returned an empty assistant message.");
            }

            var usage = root.TryGetProperty("usage", out var usageElement)
                ? usageElement
                : default;

            return new AiChatResult
            {
                Content = content,
                Model = root.TryGetProperty("model", out var modelElement) &&
                        modelElement.ValueKind == JsonValueKind.String
                    ? modelElement.GetString() ?? _options.Model
                    : _options.Model,
                PromptTokens = ReadOptionalInt(usage, "prompt_tokens"),
                CompletionTokens = ReadOptionalInt(usage, "completion_tokens"),
                TotalTokens = ReadOptionalInt(usage, "total_tokens")
            };
        }
        catch (JsonException exception)
        {
            throw new AiProviderException(
                "Qwen returned an invalid JSON response.",
                (int)response.StatusCode,
                exception);
        }
    }

    private static AiProviderException CreateProviderError(HttpStatusCode statusCode, string responseBody, string apiKey)
    {
        var detail = "No error details were returned.";
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (document.RootElement.TryGetProperty("error", out var error) &&
                error.ValueKind == JsonValueKind.Object &&
                error.TryGetProperty("message", out var message) &&
                message.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(message.GetString()))
            {
                detail = message.GetString()!;
            }
        }
        catch (JsonException)
        {
            // Do not echo arbitrary response bodies into logs or user-facing errors.
        }

        if (!string.IsNullOrEmpty(apiKey))
        {
            detail = detail.Replace(apiKey, "[redacted]", StringComparison.Ordinal);
        }

        if (detail.Length > 300)
        {
            detail = detail[..300];
        }

        return new AiProviderException(
            $"Qwen request failed with HTTP {(int)statusCode}: {detail}",
            (int)statusCode);
    }

    private static int? ReadOptionalInt(JsonElement usage, string propertyName)
    {
        if (usage.ValueKind == JsonValueKind.Object &&
            usage.TryGetProperty(propertyName, out var value) &&
            value.TryGetInt32(out var result))
        {
            return result;
        }

        return null;
    }
}
