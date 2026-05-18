// -----------------------------------------------------------------------
// <copyright file="AzureOpenAIHttpClientTests.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.AzureOpenAI.Http;
using Compendium.Adapters.AzureOpenAI.Http.Models;
using Compendium.Adapters.AzureOpenAI.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace Compendium.Adapters.AzureOpenAI.Tests.Http;

public class AzureOpenAIHttpClientTests
{
    [Fact]
    public void Ctor_WithApiKeyAuth_StampsApiKeyHeader()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        var inner = new HttpClient(handler);
        var options = TestFactories.DefaultOptions();

        // Act
        var sut = new AzureOpenAIHttpClient(inner, Options.Create(options), NullLogger<AzureOpenAIHttpClient>.Instance);

        // Assert
        sut.Should().NotBeNull();
        inner.BaseAddress!.ToString().Should().StartWith("https://test-resource.openai.azure.com");
        inner.DefaultRequestHeaders.GetValues("api-key").Should().Contain(TestFactories.DefaultApiKey);
    }

    [Fact]
    public void Ctor_WithEntraIDAuth_DoesNotStampApiKeyHeader()
    {
        // Arrange — Entra ID mode relies on a DelegatingHandler that adds the bearer token per-request.
        var handler = new MockHttpMessageHandler();
        var inner = new HttpClient(handler);
        var options = TestFactories.DefaultOptions(o =>
        {
            o.AuthMode = AzureOpenAIAuthMode.EntraID;
            o.ApiKey = null;
        });

        // Act
        _ = new AzureOpenAIHttpClient(inner, Options.Create(options), NullLogger<AzureOpenAIHttpClient>.Instance);

        // Assert
        inner.DefaultRequestHeaders.Contains("api-key").Should().BeFalse();
    }

    [Fact]
    public void Ctor_WithApiKeyAuthButNoKey_DoesNotStampHeader()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        var inner = new HttpClient(handler);
        var options = TestFactories.DefaultOptions(o =>
        {
            o.AuthMode = AzureOpenAIAuthMode.ApiKey;
            o.ApiKey = null;
        });

        // Act
        _ = new AzureOpenAIHttpClient(inner, Options.Create(options), NullLogger<AzureOpenAIHttpClient>.Instance);

        // Assert
        inner.DefaultRequestHeaders.Contains("api-key").Should().BeFalse();
    }

    [Fact]
    public void Ctor_DoesNotOverridePreSetBaseAddress()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        var inner = new HttpClient(handler) { BaseAddress = new Uri("https://proxy.test/v1/") };
        var options = TestFactories.DefaultOptions();

        // Act
        _ = new AzureOpenAIHttpClient(inner, Options.Create(options), NullLogger<AzureOpenAIHttpClient>.Instance);

        // Assert
        inner.BaseAddress!.ToString().Should().Be("https://proxy.test/v1/");
    }

    [Fact]
    public void Ctor_WithEmptyEndpoint_LeavesBaseAddressNull()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        var inner = new HttpClient(handler);
        var options = TestFactories.DefaultOptions(o => o.Endpoint = string.Empty);

        // Act
        _ = new AzureOpenAIHttpClient(inner, Options.Create(options), NullLogger<AzureOpenAIHttpClient>.Instance);

        // Assert
        inner.BaseAddress.Should().BeNull();
    }

    [Fact]
    public void Ctor_NullHttpClient_Throws()
    {
        // Arrange
        var options = TestFactories.DefaultOptions();

        // Act
        var act = () => new AzureOpenAIHttpClient(null!, Options.Create(options), NullLogger<AzureOpenAIHttpClient>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Ctor_NullOptions_Throws()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();

        // Act
        var act = () => new AzureOpenAIHttpClient(new HttpClient(handler), null!, NullLogger<AzureOpenAIHttpClient>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Ctor_NullLogger_Throws()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        var options = TestFactories.DefaultOptions();

        // Act
        var act = () => new AzureOpenAIHttpClient(new HttpClient(handler), Options.Create(options), null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateChatCompletionAsync_BuildsAzureUrlWithDeploymentAndApiVersion()
    {
        // Arrange
        var (client, handler, _) = TestFactories.CreateHttpClient();
        string? capturedUrl = null;
        handler.When(HttpMethod.Post, "*/openai/deployments/*/chat/completions*")
            .With(req => { capturedUrl = req.RequestUri!.ToString(); return true; })
            .Respond("application/json", """{"id":"x","model":"m","created":0,"choices":[]}""");

        var request = new AzureOpenAIChatCompletionRequest
        {
            Messages = new List<AzureOpenAIChatMessage> { new() { Role = "user", Content = "hi" } }
        };

        // Act
        await client.CreateChatCompletionAsync("my-deployment", request, CancellationToken.None);

        // Assert
        capturedUrl.Should().NotBeNull();
        capturedUrl!.Should().Contain("/openai/deployments/my-deployment/chat/completions");
        capturedUrl!.Should().Contain("api-version=2024-10-21");
    }

    [Fact]
    public async Task CreateChatCompletionAsync_HonoursCustomApiVersion()
    {
        // Arrange
        var (client, handler, _) = TestFactories.CreateHttpClient(o => o.ApiVersion = "2024-08-01-preview");
        string? capturedUrl = null;
        handler.When(HttpMethod.Post, "*/openai/deployments/*/chat/completions*")
            .With(req => { capturedUrl = req.RequestUri!.ToString(); return true; })
            .Respond("application/json", """{"id":"x","model":"m","created":0,"choices":[]}""");

        // Act
        await client.CreateChatCompletionAsync("d", new AzureOpenAIChatCompletionRequest
        {
            Messages = new List<AzureOpenAIChatMessage> { new() { Role = "user", Content = "hi" } }
        }, CancellationToken.None);

        // Assert
        capturedUrl!.Should().Contain("api-version=2024-08-01-preview");
    }

    [Fact]
    public async Task CreateChatCompletionAsync_EscapesDeploymentName()
    {
        // Arrange
        var (client, handler, _) = TestFactories.CreateHttpClient();
        string? capturedUrl = null;
        handler.When(HttpMethod.Post, "*/openai/deployments/*/chat/completions*")
            .With(req => { capturedUrl = req.RequestUri!.AbsoluteUri; return true; })
            .Respond("application/json", """{"id":"x","model":"m","created":0,"choices":[]}""");

        // Act
        await client.CreateChatCompletionAsync("dep with space", new AzureOpenAIChatCompletionRequest
        {
            Messages = new List<AzureOpenAIChatMessage> { new() { Role = "user", Content = "hi" } }
        }, CancellationToken.None);

        // Assert — AbsoluteUri preserves percent-encoding.
        capturedUrl.Should().NotBeNull();
        capturedUrl!.Should().Contain("dep%20with%20space");
    }

    [Fact]
    public async Task CreateEmbeddingsAsync_OnInvalidJsonResponse_ReturnsProviderError()
    {
        // Arrange
        var (client, handler, _) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/openai/deployments/*/embeddings*").Respond("application/json", "not json");

        // Act
        var result = await client.CreateEmbeddingsAsync(
            "d",
            new AzureOpenAIEmbeddingsRequest { Input = new List<string> { "x" } },
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.ProviderError");
    }

    [Fact]
    public async Task CreateEmbeddingsAsync_OnHttpRequestException_ReturnsProviderError()
    {
        // Arrange
        var (client, handler, _) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/openai/deployments/*/embeddings*").Throw(new HttpRequestException("nope"));

        // Act
        var result = await client.CreateEmbeddingsAsync(
            "d",
            new AzureOpenAIEmbeddingsRequest { Input = new List<string> { "x" } },
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.ProviderError");
    }

    [Fact]
    public async Task CreateEmbeddingsAsync_WhenCallerCancels_RethrowsCancellation()
    {
        // Arrange
        var (client, handler, _) = TestFactories.CreateHttpClient();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        handler.When(HttpMethod.Post, "*/openai/deployments/*/embeddings*").Throw(new TaskCanceledException("c"));

        // Act
        var act = async () => await client.CreateEmbeddingsAsync(
            "d",
            new AzureOpenAIEmbeddingsRequest { Input = new List<string> { "x" } },
            cts.Token);

        // Assert
        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    [Fact]
    public async Task CreateEmbeddingsAsync_OnNonCancellationTimeout_ReturnsTimeout()
    {
        // Arrange
        var (client, handler, _) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/openai/deployments/*/embeddings*").Throw(new TaskCanceledException("server slow"));

        // Act
        var result = await client.CreateEmbeddingsAsync(
            "d",
            new AzureOpenAIEmbeddingsRequest { Input = new List<string> { "x" } },
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.Timeout");
    }

    [Fact]
    public async Task LogsRequestAndResponse_WhenEnableLoggingTrue()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        var options = TestFactories.DefaultOptions(o => o.EnableLogging = true);
        var inner = new HttpClient(handler) { BaseAddress = new Uri(options.Endpoint.TrimEnd('/') + "/") };
        var logger = new RecordingLogger<AzureOpenAIHttpClient>();
        var client = new AzureOpenAIHttpClient(inner, Options.Create(options), logger);

        handler.When(HttpMethod.Post, "*/openai/deployments/*/chat/completions*")
            .Respond("application/json", """{"id":"x","model":"m","created":0,"choices":[]}""");

        // Act
        await client.CreateChatCompletionAsync(
            "d",
            new AzureOpenAIChatCompletionRequest
            {
                Messages = new List<AzureOpenAIChatMessage> { new() { Role = "user", Content = "hi" } }
            },
            CancellationToken.None);

        // Assert
        logger.Entries.Should().Contain(e => e.Message.Contains("Azure OpenAI request:"));
        logger.Entries.Should().Contain(e => e.Message.Contains("Azure OpenAI response"));
    }

    [Fact]
    public async Task LogsEmbeddingsRequest_WhenEnableLoggingTrue()
    {
        // Arrange
        var handler = new MockHttpMessageHandler();
        var options = TestFactories.DefaultOptions(o => o.EnableLogging = true);
        var inner = new HttpClient(handler) { BaseAddress = new Uri(options.Endpoint.TrimEnd('/') + "/") };
        var logger = new RecordingLogger<AzureOpenAIHttpClient>();
        var client = new AzureOpenAIHttpClient(inner, Options.Create(options), logger);

        handler.When(HttpMethod.Post, "*/openai/deployments/*/embeddings*")
            .Respond("application/json", """{"model":"m","data":[],"usage":{"prompt_tokens":0,"total_tokens":0}}""");

        // Act
        await client.CreateEmbeddingsAsync(
            "d",
            new AzureOpenAIEmbeddingsRequest { Input = new List<string> { "x" } },
            CancellationToken.None);

        // Assert
        logger.Entries.Should().Contain(e => e.Message.Contains("Azure OpenAI embeddings request:"));
    }

    [Fact]
    public async Task PingDeploymentAsync_OnHttpSuccess_ReturnsSuccess()
    {
        // Arrange
        var (client, handler, _) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/openai/deployments/*/chat/completions*")
            .Respond("application/json", """{"id":"x","model":"m","created":0,"choices":[{"index":0,"message":{"role":"assistant","content":"pong"},"finish_reason":"stop"}]}""");

        // Act
        var result = await client.PingDeploymentAsync("d", CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task PingDeploymentAsync_OnFailure_PropagatesError()
    {
        // Arrange
        var (client, handler, _) = TestFactories.CreateHttpClient();
        handler.When(HttpMethod.Post, "*/openai/deployments/*/chat/completions*")
            .Respond(HttpStatusCode.Unauthorized, "application/json", """{"error":{"message":"bad key"}}""");

        // Act
        var result = await client.PingDeploymentAsync("d", CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AI.InvalidApiKey");
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception), exception));

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
