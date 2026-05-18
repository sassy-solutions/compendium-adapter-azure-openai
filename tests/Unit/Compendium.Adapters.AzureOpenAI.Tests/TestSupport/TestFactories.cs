// -----------------------------------------------------------------------
// <copyright file="TestFactories.cs" company="Sassy Solutions">
//     Copyright (c) 2026 Sassy Solutions. Licensed under the MIT License.
//     See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------------

using Compendium.Adapters.AzureOpenAI.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Compendium.Adapters.AzureOpenAI.Tests.TestSupport;

internal static class TestFactories
{
    public const string DefaultEndpoint = "https://test-resource.openai.azure.com";
    public const string DefaultApiKey = "test-azure-api-key";
    public const string DefaultDeployment = "test-deployment";

    public static AzureOpenAIOptions DefaultOptions(Action<AzureOpenAIOptions>? configure = null)
    {
        var options = new AzureOpenAIOptions
        {
            Endpoint = DefaultEndpoint,
            ApiVersion = AzureOpenAIOptions.DefaultApiVersion,
            AuthMode = AzureOpenAIAuthMode.ApiKey,
            ApiKey = DefaultApiKey,
            DefaultModel = "gpt-4o-mini",
            DefaultEmbeddingModel = "text-embedding-3-small",
            DefaultMaxTokens = 4096,
            TimeoutSeconds = 120,
            EnableLogging = false,
            DeploymentMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["gpt-4o-mini"] = DefaultDeployment,
                ["text-embedding-3-small"] = DefaultDeployment
            }
        };
        configure?.Invoke(options);
        return options;
    }

    public static (AzureOpenAIHttpClient Client, MockHttpMessageHandler Handler, AzureOpenAIOptions Options) CreateHttpClient(
        Action<AzureOpenAIOptions>? configure = null)
    {
        var handler = new MockHttpMessageHandler();
        var options = DefaultOptions(configure);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(options.Endpoint.TrimEnd('/') + "/")
        };
        var sut = new AzureOpenAIHttpClient(
            httpClient,
            Options.Create(options),
            NullLogger<AzureOpenAIHttpClient>.Instance);
        return (sut, handler, options);
    }

    public static AzureOpenAIProvider CreateProvider(
        AzureOpenAIHttpClient httpClient,
        Action<AzureOpenAIOptions>? configure = null)
    {
        var options = DefaultOptions(configure);
        return new AzureOpenAIProvider(
            httpClient,
            Options.Create(options),
            NullLogger<AzureOpenAIProvider>.Instance);
    }

    public static (AzureOpenAIProvider Provider, MockHttpMessageHandler Handler) CreateProviderWithMock(
        Action<AzureOpenAIOptions>? configure = null)
    {
        var (httpClient, handler, _) = CreateHttpClient(configure);
        var provider = CreateProvider(httpClient, configure);
        return (provider, handler);
    }

    public static CompletionRequest SimpleCompletionRequest(string? model = null) =>
        new()
        {
            Model = model ?? "gpt-4o-mini",
            Messages = new List<Message> { Message.User("Hello") }
        };

    public static EmbeddingRequest SimpleEmbeddingRequest(int n = 1, string? model = null)
    {
        return new EmbeddingRequest
        {
            Model = model ?? "text-embedding-3-small",
            Inputs = Enumerable.Range(0, n).Select(i => $"input-{i}").ToList()
        };
    }
}
