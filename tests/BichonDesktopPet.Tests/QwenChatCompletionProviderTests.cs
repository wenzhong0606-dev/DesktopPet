using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BichonDesktopPet.Core.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class QwenChatCompletionProviderTests
{
    [TestMethod]
    public async Task CompleteAsync_SendsCompatibleRequestAndParsesUsage()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "model": "qwen-plus",
                  "choices": [
                    {
                      "message": {
                        "role": "assistant",
                        "content": "主人，豆包在这里。"
                      }
                    }
                  ],
                  "usage": {
                    "prompt_tokens": 42,
                    "completion_tokens": 12,
                    "total_tokens": 54
                  }
                }
                """)
        });
        using var client = new HttpClient(handler);
        var provider = new QwenChatCompletionProvider(
            client,
            new QwenOptions { ApiKey = "test-secret" });

        var result = await provider.CompleteAsync(new AiChatRequest
        {
            Messages =
            [
                new AiChatMessage("system", "你是豆包。"),
                new AiChatMessage("user", "你好")
            ],
            MaxTokens = 256,
            Temperature = 0.7
        });

        Assert.AreEqual("主人，豆包在这里。", result.Content);
        Assert.AreEqual("qwen-plus", result.Model);
        Assert.AreEqual(42, result.PromptTokens);
        Assert.AreEqual(12, result.CompletionTokens);
        Assert.AreEqual(54, result.TotalTokens);
        Assert.AreEqual(HttpMethod.Post, handler.Method);
        Assert.AreEqual(QwenOptions.DefaultEndpoint, handler.RequestUri?.ToString());
        Assert.AreEqual("Bearer", handler.AuthorizationScheme);
        Assert.AreEqual("test-secret", handler.AuthorizationParameter);

        using var requestJson = JsonDocument.Parse(handler.RequestBody!);
        Assert.AreEqual("qwen-plus", requestJson.RootElement.GetProperty("model").GetString());
        Assert.AreEqual(2, requestJson.RootElement.GetProperty("messages").GetArrayLength());
        Assert.AreEqual(256, requestJson.RootElement.GetProperty("max_tokens").GetInt32());
        Assert.AreEqual(0.7, requestJson.RootElement.GetProperty("temperature").GetDouble());
    }

    [TestMethod]
    public async Task CompleteAsync_WhenApiKeyIsMissing_FailsBeforeSendingRequest()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(handler);
        var provider = new QwenChatCompletionProvider(client, new QwenOptions());

        await Assert.ThrowsExceptionAsync<AiProviderNotConfiguredException>(() =>
            provider.CompleteAsync(new AiChatRequest
            {
                Messages = [new AiChatMessage("user", "你好")]
            }));

        Assert.AreEqual(0, handler.RequestCount);
    }

    [TestMethod]
    public async Task CompleteAsync_WhenApiReturnsError_ExposesStatusWithoutLeakingKey()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent(
                "{\"error\":{\"message\":\"Invalid API key\"}}")
        });
        using var client = new HttpClient(handler);
        var provider = new QwenChatCompletionProvider(
            client,
            new QwenOptions { ApiKey = "do-not-leak-this-secret" });

        var exception = await Assert.ThrowsExceptionAsync<AiProviderException>(() =>
            provider.CompleteAsync(new AiChatRequest
            {
                Messages = [new AiChatMessage("user", "你好")]
            }));

        Assert.AreEqual(401, exception.StatusCode);
        StringAssert.Contains(exception.Message, "Invalid API key");
        Assert.IsFalse(exception.Message.Contains("do-not-leak-this-secret", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task CompleteAsync_RejectsToolRoleBeforeSendingRequest()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(handler);
        var provider = new QwenChatCompletionProvider(
            client,
            new QwenOptions { ApiKey = "test-secret" });

        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            provider.CompleteAsync(new AiChatRequest
            {
                Messages = [new AiChatMessage("tool", "run arbitrary code")]
            }));

        Assert.AreEqual(0, handler.RequestCount);
    }

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        public HttpMethod? Method { get; private set; }

        public Uri? RequestUri { get; private set; }

        public string? AuthorizationScheme { get; private set; }

        public string? AuthorizationParameter { get; private set; }

        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            Method = request.Method;
            RequestUri = request.RequestUri;
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            RequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return responseFactory(request);
        }
    }
}
